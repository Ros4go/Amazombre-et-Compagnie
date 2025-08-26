using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class AmazombrePlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CharacterController controller;
    [SerializeField] Camera playerCamera;

    [Header("Input (New Input System)")]
    [SerializeField] InputActionReference move;   // Vector2
    [SerializeField] InputActionReference look;   // Vector2
    [SerializeField] InputActionReference jump;   // Button
    [SerializeField] InputActionReference dash;   // Button
    [SerializeField] InputActionReference crouch; // Button (hold/press)

    [Header("Movement")]
    [SerializeField] float runSpeed = 12f;
    [SerializeField] float accelGround = 48f;
    [SerializeField] float accelAir = 28f;         // n'agit que si input (sinon inertie)
    [SerializeField] float gravity = -30f;
    [SerializeField] float jumpHeight = 2.6f;      // saut plus haut
    [SerializeField] float coyoteTime = 0.15f;
    [SerializeField] float jumpBuffer = 0.12f;
    [SerializeField] int maxJumps = 2;

    [Header("Jump Shaping")]
    [SerializeField] float upGravityMult = 0.75f; // ascension: rapide -> lent (apex)
    [SerializeField] float apexHangMult = 0.50f;
    [SerializeField] float apexThreshold = 2.5f;
    [SerializeField] float downGravityMult = 2.00f; // descente: lent -> rapide
    [SerializeField] float maxRiseSpeed = 32f;
    [SerializeField] float maxFallSpeed = -58f;

    [Header("Dash")]
    [SerializeField] float dashSpeed = 20f;        // dash horizontal adouci
    [SerializeField] float dashDuration = 0.28f;   // sustain un peu plus long
    [SerializeField] float dashCooldown = 0.35f;
    [SerializeField] float dashSustainAccel = 45f; // poussée soutenue
    [SerializeField] float dashUpwardScale = 0.55f; // dash vertical atténué
    [SerializeField] float dashDownwardScale = 0.80f; // dash vers le bas atténué

    [Header("Slide / Crouch")]
    [SerializeField] float crouchHeight = 1.0f;
    [SerializeField] float slideMinSpeed = 9f;
    [SerializeField] float slideMaxSpeed = 24f;     // plafond soft (progressif)
    [SerializeField] float slideAccel = 14f;     // accélération propre du slide
    [SerializeField] float slideFriction = 1.5f;    // perte graduelle
    [SerializeField] float slideJumpBoost = 1.15f;
    [SerializeField] float slideTurnRate = 9f;
    [SerializeField] float slideStrafeRatio = 0.35f;
    [SerializeField] float slideGroundGrace = 0.08f; // tolérance micro-décrochage sol
    [SerializeField] float slideReenterLock = 0.12f; // lock ré-entrée après un jump

    [Header("Stick to Ground / Slopes")]
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float groundCheckDist = 0.5f;    // portée du check sol
    [SerializeField] float stickDownForce = 18f;     // tire vers le bas quand on slide (si sol OK et sol devant)
    [SerializeField] float maxSlopeAngle = 65f;     // limite logique pour le suivi
    [SerializeField] float slopeAccelScale = 0.35f;   // NEW: intensité de l'accélération due à la pente (proportionnelle à |g|)

    [Header("Slam (air)")]
    [SerializeField] float slamDownSpeed = 40f;

    [Header("Camera")]
    [SerializeField] float sensX = 0.45f;
    [SerializeField] float sensY = 0.40f;
    [SerializeField] float baseFov = 100f;
    [SerializeField] float dashFovKick = 16f;
    [SerializeField] float slideFovKick = 12f;
    [SerializeField] float speedFovScale = 0.60f;
    [SerializeField] float maxSpeedFov = 22f;
    [SerializeField] float fovLerp = 6f;            // plus lent => plus long
    [SerializeField] float slideTilt = 12f;
    [SerializeField] float tiltLerp = 6f;           // plus lent => plus long
    [SerializeField] float dashViewKick = 7f;
    [SerializeField] float viewKickDecay = 5f;

    [Header("Headroom Check")]
    [SerializeField] LayerMask headBlockMask = ~0;

    [Header("Momentum Reset")]
    [SerializeField] float directionResetAngle = 100f; // angle au-delà duquel on reset l'inertie si l'input va à l'opposé
    [SerializeField] float wallResetDamp = 0f;         // 0 = stop net de l'horizontale à l'impact mur (peut mettre 0.2 pour amortir)

    // --- State ---
    Vector3 velocity;
    bool grounded, wasGrounded;
    float lastGroundedTime;
    float jumpBufferTime;

    bool wantCrouch;
    bool crouchPressedThisFrame;
    bool isSliding;
    bool isDashing;
    bool isSlam;

    int jumpsRemaining;
    float dashTimer;
    float dashCdTimer;
    Vector3 dashDir;       // direction 3D du dash (caméra)
    Vector3 dashDirHoriz;  // pour sustain (horiz-only)
    Vector3 slideDir;
    float slideUngroundedTimer; // hors-sol pendant un slide (grâce)
    float slideReenterLockTimer; // bloque ré-entrée auto après jump

    // Ground info
    bool hasGroundHit;
    RaycastHit groundHit;
    Vector3 groundNormal = Vector3.up;
    float groundSlopeAngle;

    float defaultHeight;
    Vector3 defaultCenter;

    // Camera state
    float yaw, pitch;
    float camRoll;
    float viewKick;

    // Speed debug
    float topHorizSpeed, topVertSpeedAbs, topTotalSpeed;

    // Cache
    float cosResetThreshold;

    void Awake()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        if (!playerCamera) playerCamera = Camera.main;

        defaultHeight = controller.height;
        defaultCenter = controller.center;

        jumpsRemaining = maxJumps;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cosResetThreshold = Mathf.Cos(directionResetAngle * Mathf.Deg2Rad);
    }

    void OnEnable() { move?.action.Enable(); look?.action.Enable(); jump?.action.Enable(); dash?.action.Enable(); crouch?.action.Enable(); }
    void OnDisable() { move?.action.Disable(); look?.action.Disable(); jump?.action.Disable(); dash?.action.Disable(); crouch?.action.Disable(); }

    void Update()
    {
        float dt = Time.deltaTime;

        ReadCamera(dt);
        ReadGrounding();
        ProbeGround();
        ReadInputs();

        HandleJump(dt);
        HandleDash(dt);
        HandleSlideAndSlam(dt);
        HandleMove(dt);

        ApplyGravity(dt);
        ApplyMotion(dt);
        UpdateCameraFX(dt);
        UpdateSpeedDebug();
    }

    // ---------------- CAMERA ----------------
    void ReadCamera(float dt)
    {
        Vector2 m = look ? look.action.ReadValue<Vector2>() : Vector2.zero;
        yaw += m.x * sensX;
        pitch -= m.y * sensY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        float pitchWithKick = pitch + viewKick;
        playerCamera.transform.localRotation = Quaternion.Euler(pitchWithKick, 0f, camRoll);
    }

    void UpdateCameraFX(float dt)
    {
        float speed = velocity.Horizontal().magnitude;
        float speedKick = Mathf.Min(speed * speedFovScale, maxSpeedFov);
        float targetFov = baseFov + speedKick + (isDashing ? dashFovKick : 0f) + (isSliding ? slideFovKick : 0f);
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, 1f - Mathf.Exp(-fovLerp * dt));

        float lateralAngle = Vector3.SignedAngle(transform.forward, velocity.Flat(), Vector3.up) * 0.16f;
        float targetRoll = isSliding ? Mathf.Clamp(lateralAngle, -slideTilt, slideTilt) : 0f;
        camRoll = Mathf.Lerp(camRoll, targetRoll, 1f - Mathf.Exp(-tiltLerp * dt));

        viewKick = Mathf.Lerp(viewKick, 0f, 1f - Mathf.Exp(-viewKickDecay * dt));
    }

    // --------------- GROUNDING ---------------
    void ReadGrounding()
    {
        wasGrounded = grounded;
        grounded = controller.isGrounded;

        if (grounded) slideUngroundedTimer = 0f;
        else slideUngroundedTimer += Time.deltaTime;

        if (slideReenterLockTimer > 0f) slideReenterLockTimer -= Time.deltaTime;

        if (grounded) { lastGroundedTime = Time.time; jumpsRemaining = maxJumps; }

        // slam → slide si maintien
        if (!wasGrounded && grounded && isSlam)
        {
            isSlam = false;
            if (wantCrouch) EnterSlide(autoForwardIfNoInput: true);
        }
    }
    bool HasCoyote => Time.time - lastGroundedTime <= coyoteTime;

    // --- Ground probe ---
    void ProbeGround()
    {
        Vector3 worldCenter = transform.position + controller.center;
        float castRadius = Mathf.Max(0.01f, controller.radius * 0.95f);
        float castDist = groundCheckDist + controller.skinWidth + 0.1f;

        hasGroundHit = Physics.SphereCast(
            worldCenter + Vector3.up * 0.1f,
            castRadius,
            Vector3.down,
            out groundHit,
            castDist,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (hasGroundHit)
        {
            groundNormal = groundHit.normal;
            groundSlopeAngle = Vector3.Angle(groundNormal, Vector3.up);
        }
        else
        {
            groundNormal = Vector3.up;
            groundSlopeAngle = 0f;
        }
    }

    bool GroundOkForStick() => hasGroundHit && groundSlopeAngle <= maxSlopeAngle;

    bool HasGroundAhead()
    {
        Vector3 fwdDir = (slideDir.sqrMagnitude > 0 ? slideDir.normalized : transform.forward);
        Vector3 ahead = transform.position + controller.center + fwdDir * (controller.radius * 1.2f) + Vector3.up * 0.1f;
        float dist = groundCheckDist + controller.skinWidth + 0.2f;
        if (Physics.Raycast(ahead, Vector3.down, out var hit, dist, groundMask, QueryTriggerInteraction.Ignore))
            return Vector3.Angle(hit.normal, Vector3.up) <= maxSlopeAngle;
        return false;
    }

    // --------------- INPUTS ------------------
    void ReadInputs()
    {
        wantCrouch = crouch && crouch.action.IsPressed();
        crouchPressedThisFrame = crouch && crouch.action.triggered;

        if (jump && jump.action.triggered) jumpBufferTime = jumpBuffer;
        if (dash && dash.action.triggered && dashCdTimer <= 0f && !isDashing) StartDash();
    }

    // --------------- JUMP / COYOTE / BUFFER ---------------
    void HandleJump(float dt)
    {
        if (jumpBufferTime > 0f) jumpBufferTime -= dt;
        if (jumpBufferTime <= 0f) return;

        if (grounded || HasCoyote)
        {
            DoJump(isFromSlide: isSliding);
            jumpsRemaining = Mathf.Max(0, maxJumps - 1);
            jumpBufferTime = 0f;
            return;
        }
        if (!grounded && jumpsRemaining > 0)
        {
            DoJump(isFromSlide: false);
            jumpsRemaining--;
            jumpBufferTime = 0f;
        }
    }

    void DoJump(bool isFromSlide)
    {
        float boost = isFromSlide ? slideJumpBoost : 1f;
        float jumpVel = Mathf.Sqrt(-2f * gravity * jumpHeight);
        velocity.y = Mathf.Min(jumpVel, maxRiseSpeed);
        velocity = velocity.Horizontal() * boost + Vector3.up * velocity.y;

        ExitSlide();
        isSlam = false;

        slideReenterLockTimer = slideReenterLock; // empêche re-entrée auto immédiate
    }

    // --------------- DASH --------------------
    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCdTimer = dashCooldown + dashDuration;

        Vector3 f = (playerCamera ? playerCamera.transform.forward : transform.forward).normalized;

        // Reset d'inertie si dash opposé à la vitesse actuelle (évite superpositions incohérentes)
        Vector3 horiz = velocity.Horizontal();
        if (horiz.sqrMagnitude > 0.01f && Vector3.Dot(horiz.normalized, f.Flat()) < cosResetThreshold)
            ResetHorizontalMomentum(false); // vers baseline run

        float upDot = Vector3.Dot(f, Vector3.up);
        dashDirHoriz = Vector3.ProjectOnPlane(f, Vector3.up).normalized;
        dashDir = f;

        float horizImpulse = dashSpeed;
        float vertImpulse = upDot * dashSpeed * (upDot >= 0f ? dashUpwardScale : dashDownwardScale);

        Vector3 impulse = dashDirHoriz * horizImpulse + Vector3.up * vertImpulse;

        velocity += impulse;
        velocity.y = Mathf.Min(velocity.y, maxRiseSpeed);

        viewKick += -dashViewKick;
        // on ne coupe pas le slide (flow)
    }

    void HandleDash(float dt)
    {
        if (dashCdTimer > 0f) dashCdTimer -= dt;
        if (!isDashing) return;

        dashTimer -= dt;
        if (dashTimer <= 0f) { isDashing = false; }
        else
        {
            velocity += dashDirHoriz * (dashSustainAccel * dt); // sustain horizontal only
        }
    }

    // --------------- SLIDE / CROUCH / SLAM ---------------
    void HandleSlideAndSlam(float dt)
    {
        // SLAM uniquement sur press en l'air
        if (!grounded && crouchPressedThisFrame) isSlam = true;
        if (isSlam && velocity.y > -slamDownSpeed) velocity.y = -slamDownSpeed;

        // SLIDE au sol sur press
        if (grounded && crouchPressedThisFrame && !isSliding && !isSlam)
            EnterSlide(autoForwardIfNoInput: true);

        // Auto re-enter si crouch maintenu (sauf lock après jump)
        if (grounded && wantCrouch && !isSliding && !isSlam &&
            slideReenterLockTimer <= 0f &&
            velocity.Horizontal().magnitude >= slideMinSpeed * 0.6f)
        {
            EnterSlide(autoForwardIfNoInput: true);
        }

        if (isSliding)
        {
            float prevSpeed = velocity.Horizontal().magnitude;

            // direction cible (avant + léger strafe)
            Vector3 forward = transform.forward.Flat();
            float lx = Mathf.Clamp(MoveInput().x, -1f, 1f);
            Vector3 desiredFlat = (forward + transform.right * (lx * slideStrafeRatio)).normalized;

            bool canStick = GroundOkForStick() && HasGroundAhead();
            Vector3 desiredDir = canStick ? Vector3.ProjectOnPlane(desiredFlat, groundNormal).normalized : desiredFlat;
            if (desiredDir.sqrMagnitude < 0.0001f) desiredDir = desiredFlat;

            // RESET d'inertie si on pivote fortement en slide
            if (Vector3.Dot(slideDir.normalized, desiredDir) < cosResetThreshold)
            {
                ResetHorizontalMomentum(true);   // baseline slide
                slideDir = desiredDir;           // aligner tout de suite
            }
            else
            {
                slideDir = Vector3.RotateTowards(slideDir, desiredDir, slideTurnRate * dt, 0f);
            }

            // vitesse: conserve l'entrée, accel vers max, au-dessus on baisse doucement
            float nextSpeed;
            if (prevSpeed > slideMaxSpeed)
                nextSpeed = Mathf.Max(prevSpeed - slideFriction * dt, slideMaxSpeed);
            else
                nextSpeed = Mathf.Clamp(prevSpeed + (slideAccel - slideFriction) * dt, slideMinSpeed, slideMaxSpeed);

            // --- Accélération progressive due à la pente (physique simple) ---
            if (canStick)
            {
                // gravité projetée sur le plan
                Vector3 gAlong = Vector3.ProjectOnPlane(Vector3.down * Mathf.Abs(gravity), groundNormal);
                if (gAlong.sqrMagnitude > 0.0001f)
                {
                    Vector3 gDir = gAlong.normalized;                        // direction descente
                    float align = Vector3.Dot(slideDir, gDir);               // +descend, -monte
                    float slopeAcc = gAlong.magnitude * slopeAccelScale;     // m/s²
                    nextSpeed += (align * slopeAcc * dt);                    // ajoute/retire progressivement
                }
            }

            // appliquer
            Vector3 horiz = slideDir.normalized * nextSpeed;
            if (canStick)
            {
                Vector3 horizOnPlane = Vector3.ProjectOnPlane(horiz, groundNormal);
                float downStick = -Mathf.Max(2f, stickDownForce);
                velocity = horizOnPlane + Vector3.up * downStick;
            }
            else
            {
                velocity = horiz + Vector3.up * velocity.y;
            }

            // sortie du slide : relâche OU hors-sol prolongé
            if (!wantCrouch || slideUngroundedTimer > slideGroundGrace)
                ExitSlideRetainCrouchIfBlocked();
        }
        else
        {
            if (!wantCrouch) TryStandUp();
        }
    }

    void EnterSlide(bool autoForwardIfNoInput)
    {
        isSliding = true;

        Vector3 initDir = CurrentMoveDirWorld(defaultForwardIfNone: autoForwardIfNoInput);
        if (initDir.sqrMagnitude < 0.0001f)
            initDir = velocity.Horizontal().sqrMagnitude > 0.01f ? velocity.Horizontal().normalized : transform.forward.Flat();

        Vector3 forward = transform.forward.Flat();
        slideDir = Vector3.RotateTowards(initDir, forward, Mathf.Deg2Rad * 999f, 0f).normalized;

        float horiz = velocity.Horizontal().magnitude;
        if (horiz < slideMinSpeed) horiz = slideMinSpeed;

        if (GroundOkForStick() && HasGroundAhead())
        {
            Vector3 horizOnPlane = Vector3.ProjectOnPlane(slideDir * horiz, groundNormal);
            velocity = horizOnPlane + Vector3.up * -Mathf.Max(2f, stickDownForce);
        }
        else
        {
            velocity = slideDir * horiz + Vector3.up * velocity.y;
        }

        SetCrouch(true);
    }

    void ExitSlide() { if (!isSliding) return; isSliding = false; }
    void ExitSlideRetainCrouchIfBlocked() { if (!isSliding) return; isSliding = false; TryStandUp(); }

    void SetCrouch(bool on)
    {
        if (on)
        {
            controller.height = Mathf.Min(controller.height, crouchHeight);
            controller.center = new Vector3(defaultCenter.x, controller.height * 0.5f, defaultCenter.z);
        }
        else
        {
            controller.height = defaultHeight;
            controller.center = defaultCenter;
        }
    }

    bool CanStandUp()
    {
        float skin = controller.skinWidth + 0.02f;
        Vector3 p1 = transform.position + Vector3.up * (crouchHeight * 0.5f + skin);
        Vector3 p2 = transform.position + Vector3.up * (defaultHeight - skin);
        float r = controller.radius - skin;
        return !Physics.CheckCapsule(p1, p2, r, headBlockMask, QueryTriggerInteraction.Ignore);
    }
    void TryStandUp() { if (controller.height < defaultHeight && CanStandUp()) SetCrouch(false); }

    // --------------- MOVE CORE ---------------
    void HandleMove(float dt)
    {
        Vector3 wish = (transform.forward * MoveInput().y + transform.right * MoveInput().x).normalized;

        // En l'air SANS input: pas d'accel (on garde l'inertie)
        bool applyAirAccel = !grounded && wish.sqrMagnitude < 0.0001f ? false : true;

        if (!isSliding && applyAirAccel)
        {
            // RESET si on change brutalement de direction (au sol ou en l'air)
            Vector3 horiz = velocity.Horizontal();
            if (wish.sqrMagnitude > 0.01f && horiz.sqrMagnitude > 0.01f &&
                Vector3.Dot(horiz.normalized, wish) < cosResetThreshold)
            {
                ResetHorizontalMomentum(false); // baseline run
            }

            float accel = grounded ? accelGround : accelAir;
            Vector3 target = wish * runSpeed;
            Vector3 delta = (target - horiz);
            float maxChange = accel * dt;
            if (delta.magnitude > maxChange) delta = delta.normalized * maxChange;
            velocity = (horiz + delta) + Vector3.up * velocity.y;
        }
    }

    void ApplyGravity(float dt)
    {
        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        else
        {
            // Si slide & sol OK & sol devant → HandleSlide gère le stick vertical
            if (isSliding && GroundOkForStick() && HasGroundAhead()) return;

            float mult = velocity.y > 0f ? upGravityMult : downGravityMult;
            if (Mathf.Abs(velocity.y) <= apexThreshold) mult *= apexHangMult;
            velocity.y += gravity * mult * dt;

            if (velocity.y > maxRiseSpeed) velocity.y = maxRiseSpeed;
            if (velocity.y < maxFallSpeed) velocity.y = maxFallSpeed;
        }
    }

    void ApplyMotion(float dt)
    {
        controller.Move(velocity * dt);

        // RESET à l'impact mur (sides)
        if ((controller.collisionFlags & CollisionFlags.Sides) != 0)
        {
            Vector3 horiz = velocity.Horizontal();
            if (horiz.sqrMagnitude > 0.0001f)
            {
                // annule (ou amortit) l'horizontale
                velocity = horiz * wallResetDamp + Vector3.up * velocity.y;
            }
        }
    }

    // --------------- HELPERS -----------------
    Vector2 MoveInput() => move ? move.action.ReadValue<Vector2>() : Vector2.zero;

    Vector3 CurrentMoveDirWorld(bool defaultForwardIfNone)
    {
        Vector2 m = MoveInput();
        Vector3 dir = (transform.forward * m.y + transform.right * m.x);
        if (dir.sqrMagnitude < 0.0001f && defaultForwardIfNone) dir = transform.forward;
        return dir.Flat();
    }

    void ResetHorizontalMomentum(bool slideBaseline)
    {
        float baseline = slideBaseline ? slideMinSpeed : runSpeed;
        Vector3 dir = velocity.Horizontal().sqrMagnitude > 0.001f ? velocity.Horizontal().normalized : transform.forward.Flat();
        velocity = dir * baseline + Vector3.up * velocity.y;
    }

    void UpdateSpeedDebug()
    {
        float h = velocity.Horizontal().magnitude;
        float vAbs = Mathf.Abs(velocity.y);
        float total = velocity.magnitude;

        if (h > topHorizSpeed) topHorizSpeed = h;
        if (vAbs > topVertSpeedAbs) topVertSpeedAbs = vAbs;
        if (total > topTotalSpeed) topTotalSpeed = total;
    }

    // --------------- DEBUG UI ---------------
    void OnGUI()
    {
        const int pad = 8; int y = pad; int line = 18;
        float h = velocity.Horizontal().magnitude;
        float v = velocity.y;
        float total = velocity.magnitude;

        GUI.Label(new Rect(pad, y, 560, 20), $"Speed H: {h:0.00} m/s   V:{v:0.00} m/s   Total:{total:0.00} m/s"); y += line;
        GUI.Label(new Rect(pad, y, 560, 20), $"Top H:{topHorizSpeed:0.00}  Top|V|:{topVertSpeedAbs:0.00}  TopTot:{topTotalSpeed:0.00}"); y += line;
        GUI.Label(new Rect(pad, y, 560, 20), $"Grounded:{grounded}  Coyote:{(HasCoyote ? "yes" : "no")}  Jumps:{jumpsRemaining}/{maxJumps}"); y += line;
        GUI.Label(new Rect(pad, y, 560, 20), $"Dash:{(isDashing ? "ON" : "off")}  CD:{Mathf.Max(0f, dashCdTimer):0.00}s"); y += line;
        GUI.Label(new Rect(pad, y, 560, 20), $"Slide:{(isSliding ? "ON" : "off")}  Slam:{(isSlam ? "ON" : "off")}  SlideGrace:{slideUngroundedTimer:0.000}/{slideGroundGrace:0.000}"); y += line;
        GUI.Label(new Rect(pad, y, 560, 20), $"Slope:{groundSlopeAngle:0.0}°  Normal:{groundNormal}"); y += line;

        DrawBar(pad, ref y, "H Speed", h / Mathf.Max(1f, slideMaxSpeed), 160, 8);
        DrawBar(pad, ref y, "Total Sp", total / Mathf.Max(1f, slideMaxSpeed * 1.25f), 160, 8);
        DrawBar(pad, ref y, "V Up", Mathf.Clamp01(Mathf.Max(0f, v) / maxRiseSpeed), 160, 8);
        DrawBar(pad, ref y, "V Down", Mathf.Clamp01(Mathf.Abs(Mathf.Min(0f, v)) / Mathf.Abs(maxFallSpeed)), 160, 8);
    }

    void DrawBar(int x, ref int y, string label, float t, int w, int h)
    {
        GUI.Label(new Rect(x, y, 120, h + 12), label);
        x += 70;
        t = Mathf.Clamp01(t);
        GUI.Box(new Rect(x, y, w, h), GUIContent.none);
        GUI.Box(new Rect(x, y, w * t, h), GUIContent.none);
        y += h + 6;
    }
}

// --------- Small extension helpers ----------
static class VecExt
{
    public static Vector3 Horizontal(this Vector3 v) => new Vector3(v.x, 0f, v.z);
    public static Vector3 Flat(this Vector3 v)
    {
        Vector3 h = new Vector3(v.x, 0f, v.z);
        return h.sqrMagnitude > 0.000001f ? h.normalized : Vector3.zero;
    }
}
