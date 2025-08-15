using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(0)]
[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    // ---------- Look ----------
    [Header("Look")]
    public float mouseSensitivity = 0.1f;
    public Transform cameraPivot; // optionnel (pitch)
    float yaw, pitch;

    // ---------- Refs ----------
    [Header("Refs")]
    public InputReader input;
    public MovementDataSO data;
    public PlayerDebug debugHUD;

    // ---------- Runtime (lecture debug) ----------
    [Header("Runtime (read-only in play)")]
    public Vector3 velocity;          // world-space
    public int jumpCount;
    public float coyoteTimer;         // temps depuis la dernière frame au sol
    public float timeInAir;           // pour gravité progressive
    public float slopeTimer;          // temps passé à glisser sur la même pente
    public bool isGrounded;
    public bool nearSteepSlope;       // proche d'une pente > CC.slopeLimit
    public Vector3 groundNormal = Vector3.up;
    public float groundDistance;
    [HideInInspector] public Vector3 lastWallNormal;
    [HideInInspector] public float lastWallTime;

    // ---------- Internes ----------
    CharacterController controller;
    StateMachine fsm = new StateMachine();
    float jumpBufferTimer;
    float initialStepOffset, lastStepOffset;
    public float gravityScale = 1f;

    // base planaire (recalculée 1x/frame)
    Vector3 planarFwd, planarRight;

    // Const offsets ground check
    const float kGroundUpOffset = 0.10f;
    const float kCloseBias = 0.15f;

    // Helpers pente
    float WalkableLimitDeg() => controller.slopeLimit;                    // limite "walkable" du CC
    public bool IsTooSteep(Vector3 n) => Vector3.Angle(n, Vector3.up) > data.slopeSlideThresholdDeg;

    public float HorizontalSpeed => Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude;
    public float SlopeAngleDeg => Vector3.Angle(groundNormal, Vector3.up);

    public void MarkLeftWall(Vector3 normal)
    {
        lastWallNormal = normal.normalized;
        lastWallTime = Time.time;
    }

    public bool CanRegrabOppositeWall(Vector3 candNormal)
    {
        // seulement dans la fenêtre de grâce
        if (Time.time - lastWallTime > data.wallRegrabGrace) return false;
        // opposé à > ~120° (dot <= -0.5)
        return Vector3.Dot(candNormal.normalized, lastWallNormal) <= -0.5f;
    }


    // ---------- Unity ----------
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (debugHUD != null) debugHUD.player = this;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yaw = transform.eulerAngles.y;

        initialStepOffset = controller.stepOffset;
        lastStepOffset = initialStepOffset;
        controller.minMoveDistance = 0f; // autorise micro-glisse

        // État initial
        fsm.ChangeState(new GroundedState(this));
    }

    void Update()
    {
        BuildPlanarBasis();

        // 1) Grounding d'abord (les States en dépendent)
        UpdateGrounding();

        // 2) States : gèrent Move + Jump
        fsm.Tick();

        // 3) Transversal : glisse + gravité
        float dt = Time.deltaTime;

        if ((isGrounded || nearSteepSlope) && IsTooSteep(groundNormal))
            ApplySlopeSlide(groundNormal, dt);
        else
            slopeTimer = 0f;

        ApplyProgressiveGravity(dt);

        // 4) Déplacement (unique)
        controller.Move(velocity * dt);

        // 5) StepOffset dynamique (évite de "remonter" une pente raide)
        SetStepOffset(IsTooSteep(groundNormal) ? 0f : initialStepOffset);

        // 6) Debug
        debugHUD?.SetDirty();
    }

    void LateUpdate() => ApplyLook();

    // ---------- Sol / Pentes ----------
    void UpdateGrounding()
    {
        // Reset simples
        groundNormal = Vector3.up;
        groundDistance = Mathf.Infinity;
        nearSteepSlope = false;

        // "Pieds" de la capsule
        Vector3 ccCenter = transform.TransformPoint(controller.center);
        float bottomOffset = controller.height * 0.5f - controller.radius;
        Vector3 feet = ccCenter + Vector3.down * (bottomOffset - 0.005f);

        // SphereCast court, tolérant
        float castRadius = controller.radius * 0.95f;
        float castDist = data.groundCheckExtra + 0.50f;

        bool closeToGround = false;

        if (Physics.SphereCast(feet + Vector3.up * kGroundUpOffset, castRadius, Vector3.down,
                               out var hit, castDist, data.groundMask, QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal;
            groundDistance = hit.distance;

            closeToGround = hit.distance <= (data.groundCheckExtra + kCloseBias);
            float angle = Vector3.Angle(groundNormal, Vector3.up);
            bool walkable = angle <= WalkableLimitDeg();

            // Confiance au CC, et on complète si très proche
            isGrounded = controller.isGrounded
                         || (closeToGround && walkable && Vector3.Dot(groundNormal, Vector3.up) > 0.05f);

            // Trop raide pour marcher mais proche = glisse potentielle
            nearSteepSlope = closeToGround && !walkable;
        }
        else
        {
            isGrounded = false; // rien sous les pieds
        }
    }

    // ---------- Accélération (utilisées par les States) ----------
    void BuildPlanarBasis()
    {
        planarFwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        planarFwd.y = 0f;
        if (planarFwd.sqrMagnitude < 1e-6f) planarFwd = Vector3.forward;
        planarFwd.Normalize();
        planarRight = new Vector3(planarFwd.z, 0f, -planarFwd.x);
    }

    public Vector3 GetWishDirectionOnPlane()
    {
        Vector2 m = input.Move;
        if (m.sqrMagnitude < 1e-6f) return Vector3.zero;
        return (planarRight * m.x + planarFwd * m.y).normalized;
    }

    public void ApplyGroundAcceleration(Vector3 wishDir, float dt)
    {
        Vector3 horizVel = Vector3.ProjectOnPlane(velocity, Vector3.up);

        // Direction souhaitée sur le plan du sol
        Vector3 wishGround = Vector3.ProjectOnPlane(wishDir, groundNormal).normalized;

        // Pénalité en montée
        Vector3 downSlope = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
        float uphill = Mathf.Clamp01(Vector3.Dot(wishGround, -downSlope)); // 0=plat/descente, 1=montée
        float speedScale = Mathf.Lerp(1f, data.uphillSpeedScale, uphill);
        float accelScale = Mathf.Lerp(1f, data.uphillAccelScale, uphill);

        float target = data.maxGroundSpeed * speedScale;
        Vector3 desired = wishGround * target;

        Vector3 delta = desired - horizVel;
        Vector3 step = Vector3.ClampMagnitude(delta, data.accelGround * accelScale * dt);

        velocity += Vector3.ProjectOnPlane(step, groundNormal);
    }

    public void ApplyAirAcceleration(Vector3 wishDir, float dt)
    {
        Vector3 horizVel = Vector3.ProjectOnPlane(velocity, Vector3.up);
        float factor = (velocity.y >= 0f) ? data.airControlAscendFactor : data.airControlDescendFactor;

        Vector3 desired = wishDir * data.maxAirSpeed;
        Vector3 delta = desired - horizVel;
        Vector3 step = Vector3.ClampMagnitude(delta, data.accelAir * factor * dt);
        velocity += Vector3.ProjectOnPlane(step, Vector3.up);
    }

    public void ApplyGroundFriction(float dt)
    {
        Vector3 horiz = Vector3.ProjectOnPlane(velocity, Vector3.up);
        if (horiz.sqrMagnitude < 1e-6f) return;

        float drop = Mathf.Min(horiz.magnitude, data.decelGround * dt);
        velocity -= horiz.normalized * drop;
    }

    // ---------- Glissade ----------
    struct SlopeInfo { public float sin; public Vector3 dir; } // dir = downhill on plane
    SlopeInfo GetSlopeInfo(Vector3 n)
    {
        float cos = Mathf.Clamp01(Vector3.Dot(n, Vector3.up));
        float sin = Mathf.Sqrt(Mathf.Max(0f, 1f - cos * cos));
        Vector3 dir = Vector3.ProjectOnPlane(Vector3.down, n).normalized;
        return new SlopeInfo { sin = sin, dir = dir };
    }

    void ApplySlopeSlide(Vector3 n, float dt)
    {
        slopeTimer += dt;

        // Coller la vitesse au plan
        velocity = Vector3.ProjectOnPlane(velocity, n);

        var s = GetSlopeInfo(n);

        // Gravité effective (progressive)
        float t = Mathf.Clamp01(timeInAir / Mathf.Max(0.0001f, data.gravityRampTime));
        float gEff = Mathf.Lerp(data.gravityBase, data.gravityMax, t) * data.slopeGravityFactor;

        // Profil d’angle + boost avec le temps
        float angleFactor = Mathf.Pow(s.sin, data.slopeAnglePower);
        float boost = (data.slopeSlideBaseAccel + data.slopeSlideAccelPerSec * slopeTimer) * angleFactor;

        Vector3 slideAccel = s.dir * (gEff * s.sin + boost);
        velocity += slideAccel * dt;

        // Clamp horizontal
        Vector3 horiz = Vector3.ProjectOnPlane(velocity, Vector3.up);
        if (horiz.magnitude > data.slopeSlideMaxSpeed)
            velocity = horiz.normalized * data.slopeSlideMaxSpeed + Vector3.up * velocity.y;
    }

    // ---------- Gravité ----------
    void ApplyProgressiveGravity(float dt)
    {
        // Collé au sol (walkable) = petite valeur constante
        if (isGrounded && !IsTooSteep(groundNormal))
        {
            velocity.y = -2f;
            return;
        }

        float t = Mathf.Clamp01(timeInAir / Mathf.Max(0.0001f, data.gravityRampTime));
        float g = Mathf.Lerp(data.gravityBase, data.gravityMax, t);
        velocity.y -= (g * gravityScale) * dt;

        // Vitesse terminale
        if (velocity.y < -data.terminalVelocity)
            velocity.y = -data.terminalVelocity;
    }

    // ---------- Sauts ----------
    public bool CanGroundJump() => isGrounded && !IsTooSteep(groundNormal);

    public bool CanJump()
    {
        bool groundedOrCoyote = isGrounded || (coyoteTimer <= data.coyoteTime);
        if (jumpCount == 0) return groundedOrCoyote;                // premier saut : au sol ou coyote
        return jumpCount > 0 && jumpCount < data.maxJumps;         // sauts aériens
    }

    public void DoJumpFromGround()
    {
        jumpBufferTimer = 0f;
        jumpCount = 1;
        timeInAir = 0f;
        float v0 = Mathf.Sqrt(2f * data.gravityBase * data.jumpHeight);
        velocity.y = v0; // set, pas add
        isGrounded = false;
    }

    public void DoJumpInAir()
    {
        jumpBufferTimer = 0f;
        jumpCount = Mathf.Max(jumpCount + 1, 1);
        timeInAir = 0f;
        float v0 = Mathf.Sqrt(2f * data.gravityBase * data.jumpHeight);
        velocity.y = v0; // set, pas add
    }

    // Buffer API (utilisé par les States)
    public void PushJumpBuffer() => jumpBufferTimer = data.jumpBuffer;
    public void ClearJumpBuffer() => jumpBufferTimer = 0f;
    public bool HasJumpBuffer => jumpBufferTimer > 0f;
    public void DecayJumpBuffer(float dt) => jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);

    // ---------- Look ----------
    void ApplyLook()
    {
        Vector2 look = input.Look;
        yaw += look.x * mouseSensitivity;
        pitch -= look.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -85f, 85f);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Toggle lock
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
    }

    // ---------- Utils ----------
    void SetStepOffset(float v)
    {
        if (Mathf.Abs(lastStepOffset - v) > 0.0001f)
        {
            controller.stepOffset = v;
            lastStepOffset = v;
        }
    }

    // ---------- API pour States (exposition) ----------
    public void ForceAddVelocity(Vector3 v) => velocity += v;
    public void SetVelocity(Vector3 v) => velocity = v;
    public CharacterController Controller => controller;
    public StateMachine FSM => fsm;
}
