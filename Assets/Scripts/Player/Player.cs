using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Look")]
    public float mouseSensitivity = 0.1f;
    public Transform cameraPivot; // optionnel (pitch)
    float yaw, pitch;

    [Header("Refs")]
    public InputReader input;
    public MovementDataSO data;
    public PlayerDebug debugHUD;

    [Header("Runtime")]
    public Vector3 velocity;       // world-space
    public int jumpCount;
    public float coyoteTimer;      // temps depuis la dernière frame au sol (reset quand grounded)
    public float timeInAir;        // pour gravité progressive
    public float slopeTimer;       // temps passé à glisser sur la même pente
    public bool isGrounded;
    public Vector3 groundNormal = Vector3.up;
    public float groundDistance;       // distance au sol sous les pieds
    public bool nearSteepSlope;        // vrai si pente raide sous les pieds

    // interne
    CharacterController controller;
    StateMachine fsm = new StateMachine();

    // buffers
    float jumpBufferTimer;
    float initialStepOffset;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (debugHUD != null) debugHUD.player = this;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yaw = transform.eulerAngles.y;

        controller = GetComponent<CharacterController>();
        initialStepOffset = controller.stepOffset;
        controller.minMoveDistance = 0f;
    }

    void Update()
    {
        fsm.Tick();

        ApplyUniversalMovement(Time.deltaTime);
        controller.Move(velocity * Time.deltaTime);
        //FaceMovementDirection();
        ApplyLook();

        debugHUD?.SetDirty();
    }

    // ---------- Universal Movement ----------
    void ApplyUniversalMovement(float dt)
    {
        UpdateGrounding();

        // --- JUMP: buffer & coyote ---
        if (input.ConsumeJumpPressed())
            jumpBufferTimer = data.jumpBuffer;

        if (isGrounded)
        {
            timeInAir = 0f;
            coyoteTimer = 0f;
            jumpCount = 0;

            if (jumpBufferTimer > 0f && CanJump())
                DoJumpFromGround();
        }
        else
        {
            coyoteTimer += dt;
            timeInAir += dt;
            if (jumpBufferTimer > 0f && CanJump())
                DoJumpInAir(); // double, triple, etc.
        }

        jumpBufferTimer -= dt;

        // --- Horizontal input ---
        Vector3 wishDir = GetWishDirectionOnPlane();
        ApplyGroundOrAirAcceleration(wishDir, dt);

        // --- Slope slide (glisse si pente trop raide), même si CC n'est pas "grounded" ---
        if ((isGrounded || nearSteepSlope) && IsTooSteep(groundNormal))
            ApplySlopeSlide(groundNormal, dt);
        else
            slopeTimer = 0f;

        // --- Gravity progressive ---
        ApplyProgressiveGravity(dt);

        // --- Freinage au sol si pas d'input ---
        if (isGrounded && !IsTooSteep(groundNormal) && wishDir.sqrMagnitude < 0.0001f)
            ApplyGroundFriction(dt);

        controller.stepOffset = (IsTooSteep(groundNormal) ? 0f : initialStepOffset);
    }

    void UpdateGrounding()
    {
        isGrounded = controller.isGrounded;
        groundNormal = Vector3.up;

        Vector3 ccCenter = transform.TransformPoint(controller.center);
        float bottomOffset = controller.height * 0.5f - controller.radius;
        Vector3 feet = ccCenter + Vector3.down * (bottomOffset - 0.01f);

        groundDistance = Mathf.Infinity;
        nearSteepSlope = false;

        // spherecast très court sous les pieds
        float castRadius = controller.radius * 0.98f;
        float castDist = data.groundCheckExtra + 0.35f;

        if (Physics.SphereCast(feet + Vector3.up * 0.05f, castRadius, Vector3.down, out var hit, castDist, data.groundMask, QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal;
            groundDistance = hit.distance;

            float slopeLimit = Mathf.Max(data.slopeSlideThresholdDeg, controller.slopeLimit);
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            bool closeToGround = hit.distance <= (data.groundCheckExtra + 0.12f);

            // solide & proche = grounded "fiable"
            isGrounded |= (Vector3.Dot(hit.normal, Vector3.up) > 0.05f) && closeToGround;

            // proche d'une pente raide
            nearSteepSlope = closeToGround && angle > slopeLimit - 0.5f;
        }
    }

    Vector3 GetWishDirectionOnPlane()
    {
        Vector2 m = input.Move;
        Vector3 camFwd = transform.forward; // simple: yaw déjà porté par le joueur
        Vector3 camRight = new Vector3(camFwd.z, 0f, -camFwd.x);
        Vector3 dir = (camRight * m.x + Vector3.ProjectOnPlane(camFwd, Vector3.up).normalized * m.y);
        return dir.normalized;
    }

    void ApplyGroundOrAirAcceleration(Vector3 wishDir, float dt)
    {
        Vector3 horizVel = Vector3.ProjectOnPlane(velocity, Vector3.up);

        if (isGrounded && !IsTooSteep(groundNormal))
        {
            float target = data.maxGroundSpeed;
            Vector3 desired = wishDir * target;
            Vector3 delta = desired - horizVel;
            Vector3 step = Vector3.ClampMagnitude(delta, data.accelGround * dt);
            velocity += Vector3.ProjectOnPlane(step, Vector3.up);
        }
        else
        {
            float vertical = velocity.y;
            float factor = vertical >= 0f ? data.airControlAscendFactor : data.airControlDescendFactor;

            Vector3 desired = wishDir * data.maxAirSpeed;
            Vector3 delta = desired - horizVel;
            Vector3 step = Vector3.ClampMagnitude(delta, data.accelAir * factor * dt);
            velocity += Vector3.ProjectOnPlane(step, Vector3.up);
        }
    }

    bool IsTooSteep(Vector3 n)
    {
        float angle = Vector3.Angle(n, Vector3.up);
        return angle > data.slopeSlideThresholdDeg;
    }

    void ApplySlopeSlide(Vector3 n, float dt)
    {
        slopeTimer += dt;

        // garder la vitesse collée au plan
        velocity = Vector3.ProjectOnPlane(velocity, n);

        // direction de glisse = gravité projetée sur le plan
        // (utilise la gravité courante pour que la glisse s'accélère "naturellement")
        float t = Mathf.Clamp01(timeInAir / Mathf.Max(0.0001f, data.gravityRampTime));
        float g = Mathf.Lerp(data.gravityBase, data.gravityMax, t);

        Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, n).normalized;
        float overLimit = Mathf.Clamp01((Vector3.Angle(n, Vector3.up) - controller.slopeLimit) / (89.9f - controller.slopeLimit));

        float accel = (data.slopeSlideBaseAccel + data.slopeSlideAccelPerSec * slopeTimer);
        Vector3 slideAccel = slopeDir * (g * 0.35f + accel) * Mathf.Max(0.15f, overLimit); // mix gravité projetée + boost

        velocity += slideAccel * dt;

        // clamp horizontal
        Vector3 horiz = Vector3.ProjectOnPlane(velocity, Vector3.up);
        if (horiz.magnitude > data.slopeSlideMaxSpeed)
            velocity = horiz.normalized * data.slopeSlideMaxSpeed + Vector3.up * velocity.y;
    }

    void ApplyProgressiveGravity(float dt)
    {
        // si au sol sur pente non raide, verrouille la verticale
        if (isGrounded && !IsTooSteep(groundNormal))
        {
            velocity.y = -2f;
            return;
        }

        float t = Mathf.Clamp01(timeInAir / Mathf.Max(0.0001f, data.gravityRampTime));
        float g = Mathf.Lerp(data.gravityBase, data.gravityMax, t);
        velocity.y -= g * dt;

        // Clamp terminal
        if (velocity.y < -data.terminalVelocity)
            velocity.y = -data.terminalVelocity;
    }

    void ApplyGroundFriction(float dt)
    {
        Vector3 horiz = Vector3.ProjectOnPlane(velocity, Vector3.up);
        float drop = Mathf.Min(horiz.magnitude, data.decelGround * dt);
        velocity -= horiz.normalized * drop;
    }

    // ---------- Jumps ----------
    bool CanJump()
    {
        // au sol (direct) ou coyote + compteurs
        bool groundedOrCoyote = isGrounded || (coyoteTimer <= data.coyoteTime);
        if (groundedOrCoyote && jumpCount == 0) return true; // premier saut
        return jumpCount < data.maxJumps;
    }

    void DoJumpFromGround()
    {
        jumpBufferTimer = 0f;
        jumpCount = 1;
        timeInAir = 0f;
        float v0 = Mathf.Sqrt(2f * data.gravityBase * data.jumpHeight);
        velocity.y = v0;         // <-- set, pas add
        isGrounded = false;
    }

    void DoJumpInAir()
    {
        jumpBufferTimer = 0f;
        jumpCount = Mathf.Max(jumpCount + 1, 1);
        timeInAir = 0f;
        float v0 = Mathf.Sqrt(2f * data.gravityBase * data.jumpHeight);
        velocity.y = v0;         // <-- set, pas add ni max()
    }

    // ---------- Facing ----------
    void FaceMovementDirection()
    {
        Vector3 horiz = Vector3.ProjectOnPlane(velocity, Vector3.up);
        if (horiz.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(horiz.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 0.2f);
        }

        // Optionnel : appliquer l’input Look ici si tu veux séparer l’orientation caméra
        // (ex: yaw += input.Look.x * sens; pitch géré ailleurs)
    }

    void ApplyLook()
    {
        Vector2 look = input.Look;
        yaw += look.x * mouseSensitivity;
        pitch -= look.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -85f, 85f);

        // Yaw sur le joueur
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Pitch sur un pivot de caméra si assigné
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Toggle lock avec Échap
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
    }

    // API pour les States (quand tu les ajouteras)
    public void ForceAddVelocity(Vector3 v) => velocity += v;
    public void SetVelocity(Vector3 v) => velocity = v;
    public CharacterController Controller => controller;
    public StateMachine FSM => fsm;
}