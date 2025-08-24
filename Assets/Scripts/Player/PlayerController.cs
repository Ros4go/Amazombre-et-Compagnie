using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(CharacterController))]
public class AmazombrePlayer : MonoBehaviour
{
    // === Références ===
    public Camera cam;
    CharacterController cc;

    // === Camera FPS ===
    float yaw, pitch;
    float lookSens = 1.2f;             // Simple, pas de courbe
    float pitchMin = -85f, pitchMax = 85f;

    // === Mouvement de base ===
    Vector3 velocity;                  // m/s (monde)
    float moveSpeed = 10f;             // vitesse cible au sol
    float airControl = 0.5f;           // autorité en l'air (0-1)
    float accel = 30f;                 // accélération au sol
    float airAccel = 12f;              // accélération en l'air
    float gravity = -25f;              // gravité custom
    float jumpForce = 7.5f;            // impulsion saut
    int maxAirJumps = 1;               // double-jump simple
    int airJumpsLeft;

    // === Dash ===
    int maxDash = 2;
    int dashLeft;
    float dashSpeed = 20f;
    float dashTime = 0.15f;
    bool isDashing;
    float dashTimer;
    Vector3 dashDir;

    // === Slide / Crouch ===
    bool isSliding;
    float crouchHeight = 1.1f;
    float normalHeight;
    float slideFriction = 0.02f;       // très faible
    Vector3 slideDir;

    // === Slam (descente rapide) ===
    bool isSlamming;
    float slamSpeed = 40f;

    // === Grappin (pull + swing simple) ===
    bool isGrappling;
    Vector3 grapplePoint;
    float grappleRange = 35f;
    float grapplePull = 22f;           // m/s "vers le point"
    float ropeLen;                     // longueur de corde "swing"
    float ropeTighten = 25f;           // rapproche la corde (pull)
    float ropeDamping = 0.02f;         // amorti léger du swing

    // === Décharge d’énergie (rocket / redirect) ===
    float blastImpulse = 12f;
    float blastProbe = 3.5f;           // distance de check mur/sol

    // === Stamina / Ressources ===
    float stamina = 1f;                // 0..1
    float staminaDrainGrapple = 0.25f; // /s
    float staminaRegenGround = 0.6f;   // /s
    float staminaRegenAir = 0.25f;     // /s

    // === HUD (simple OnGUI) ===
    float speedDisplay;                // m/s horizontal

    // === Input (New Input System, simple lecture) ===
    Vector2 moveInput;
    Vector2 lookInput;
    bool jumpPressed, dashPressed, crouchHeld, fireBlastPressed, grappleHeld;

    // === Utils ===
    LayerMask worldMask = ~0;          // tout par défaut

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!cam) cam = GetComponentInChildren<Camera>();
        normalHeight = cc.height;
        ResetGroundResources();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        ReadInputs();

        LookUpdate();

        bool grounded = cc.isGrounded;
        if (grounded)
        {
            if (isSlamming) isSlamming = false;
            if (!isSliding) ResetGroundResources();
        }

        // Dash (prend la priorité courte)
        if (dashPressed && dashLeft > 0 && !isDashing)
            StartDash();

        // Slide / Slam
        if (crouchHeld)
        {
            if (grounded) StartSlide();
            else if (!isSlamming) StartSlam();
        }
        else
        {
            if (isSliding) StopSlideIfCanStand();
        }

        // Grappin latch / release
        HandleGrapple();

        // Saut
        if (jumpPressed) DoJump(grounded);

        // Mouvement (ordres simples)
        Vector3 wishVel = WishVelocity(grounded);
        ApplyHorizontalAcceleration(wishVel, grounded);

        // Slam force
        if (isSlamming) velocity.y = -slamSpeed;

        // Dash override (pendant la fenêtre)
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            velocity = dashDir * dashSpeed + Vector3.up * velocity.y; // conserve la verticale (gravité)
            if (dashTimer <= 0f) isDashing = false;
        }

        // Slide friction au sol
        if (isSliding && grounded)
            velocity = Vector3.Lerp(velocity, new Vector3(velocity.x, velocity.y, velocity.z), 1f - slideFriction);

        // Grappin forces (pull + swing)
        if (isGrappling)
            GrappleForces();

        // Gravité
        if (!grounded && !isSlamming)
            velocity.y += gravity * Time.deltaTime;
        else if (grounded && velocity.y < 0f)
            velocity.y = -2f; // plaque au sol

        // Collision mur en slide -> stop simple
        if (isSliding && ForwardBlocked())
            isSliding = false;

        // Déplacement
        cc.Move(velocity * Time.deltaTime);

        // Vitesse HUD
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        speedDisplay = flat.magnitude;

        // Regen stamina
        RegenStamina(grounded);
    }

    // -------- Input (New Input System — Keyboard/Mouse simple) --------
    void ReadInputs()
    {
        // Mouvement (WASD / ZQSD)
        var kb = Keyboard.current;
        moveInput = Vector2.zero;
        if (kb != null)
        {
            float x = (KeyDown(kb.dKey) ? 1 : 0) - (KeyDown(kb.aKey) ? 1 : 0);
            float y = (KeyDown(kb.wKey) ? 1 : 0) - (KeyDown(kb.sKey) ? 1 : 0);
#if UNITY_EDITOR || UNITY_STANDALONE
            // ZQSD friendly (ajoute Z/Q)
            x += (KeyDown(kb.rightArrowKey) ? 1 : 0) - (KeyDown(kb.leftArrowKey) ? 1 : 0);
            y += (KeyDown(kb.upArrowKey) ? 1 : 0) - (KeyDown(kb.downArrowKey) ? 1 : 0);
            if (KeyDown(kb.qKey)) x -= 1;
            if (KeyDown(kb.zKey)) y += 1;
#endif
            moveInput = new Vector2(Mathf.Clamp(x, -1, 1), Mathf.Clamp(y, -1, 1));
            crouchHeld = KeyDown(kb.leftCtrlKey) || KeyDown(kb.cKey);
            dashPressed = WasPressed(kb.leftShiftKey) || WasPressed(kb.eKey);
            jumpPressed = WasPressed(kb.spaceKey);
            fireBlastPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            grappleHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;
        }

        // Look
        var mouse = Mouse.current;
        lookInput = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;

        // Décharge (tir énergie)
        if (fireBlastPressed)
            EnergyBlast();
    }
    bool KeyDown(KeyControl k) => k != null && k.isPressed;
    bool WasPressed(KeyControl k) => k != null && k.wasPressedThisFrame;

    // -------- Camera FPS --------
    void LookUpdate()
    {
        yaw += lookInput.x * lookSens;
        pitch -= lookInput.y * lookSens;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cam) cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // -------- Locomotion de base --------
    Vector3 WishVelocity(bool grounded)
    {
        Vector3 f = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized;
        Vector3 r = new Vector3(cam.transform.right.x, 0, cam.transform.right.z).normalized;
        Vector3 wish = (f * moveInput.y + r * moveInput.x).normalized * moveSpeed;
        return grounded ? wish : wish * airControl + new Vector3(velocity.x, 0, velocity.z) * (1f - airControl);
    }

    void ApplyHorizontalAcceleration(Vector3 wish, bool grounded)
    {
        Vector3 cur = new Vector3(velocity.x, 0, velocity.z);
        float a = grounded ? accel : airAccel;
        cur = Vector3.MoveTowards(cur, new Vector3(wish.x, 0, wish.z), a * Time.deltaTime);
        velocity.x = cur.x;
        velocity.z = cur.z;
    }

    void DoJump(bool grounded)
    {
        if (grounded)
        {
            velocity.y = jumpForce;
            airJumpsLeft = maxAirJumps;
            return;
        }
        if (airJumpsLeft > 0)
        {
            velocity.y = jumpForce;
            airJumpsLeft--;
        }
    }

    // -------- Dash --------
    void StartDash()
    {
        dashLeft--;
        isDashing = true;
        dashTimer = dashTime;

        Vector3 f = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized;
        Vector3 r = new Vector3(cam.transform.right.x, 0, cam.transform.right.z).normalized;
        Vector2 inp = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : new Vector2(0, 1);
        dashDir = (f * inp.y + r * inp.x).normalized;
    }

    // -------- Slide / Slam --------
    void StartSlide()
    {
        if (isSliding) return;
        isSliding = true;
        slideDir = new Vector3(velocity.x, 0, velocity.z).normalized;
        cc.height = crouchHeight;
        cc.center = new Vector3(0, crouchHeight * 0.5f, 0);
    }

    void StopSlideIfCanStand()
    {
        // Check simple : capsule au-dessus libre ?
        if (CanStandUp())
        {
            isSliding = false;
            cc.height = normalHeight;
            cc.center = new Vector3(0, normalHeight * 0.5f, 0);
        }
    }

    bool CanStandUp()
    {
        float extra = 0.05f;
        Vector3 top = transform.position + Vector3.up * (crouchHeight + extra);
        return !Physics.CheckCapsule(transform.position, top, cc.radius * 0.95f, worldMask, QueryTriggerInteraction.Ignore);
    }

    void StartSlam()
    {
        isSlamming = true;
    }

    bool ForwardBlocked()
    {
        Vector3 origin = transform.position + Vector3.up * (cc.height * 0.4f);
        return Physics.SphereCast(origin, cc.radius * 0.9f, transform.forward, out var hit, 0.5f, worldMask, QueryTriggerInteraction.Ignore);
    }

    // -------- Grappin --------
    void HandleGrapple()
    {
        if (grappleHeld)
        {
            if (!isGrappling)
            {
                // Latch
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, grappleRange, worldMask, QueryTriggerInteraction.Ignore))
                {
                    isGrappling = true;
                    grapplePoint = hit.point;
                    ropeLen = Vector3.Distance(GetHip(), grapplePoint); // longueur initiale = swing
                }
            }
            // drain stamina pendant latch
            if (isGrappling)
            {
                stamina = Mathf.Max(0f, stamina - staminaDrainGrapple * Time.deltaTime);
                if (stamina <= 0f) isGrappling = false;
            }
        }
        else
        {
            isGrappling = false;
        }
    }

    void GrappleForces()
    {
        Vector3 hip = GetHip();
        Vector3 toPoint = (grapplePoint - hip);
        float dist = toPoint.magnitude;
        if (dist < 1.2f) { isGrappling = false; return; }

        Vector3 dir = toPoint / Mathf.Max(dist, 0.0001f);

        // Pull: réduire peu à peu la corde
        ropeLen = Mathf.MoveTowards(ropeLen, 0.8f, ropeTighten * Time.deltaTime);

        // Swing simple : contrainte de corde (si on "dépasse" la longueur, projeter la vitesse sur la tangente)
        if (dist > ropeLen)
        {
            // enlever la composante radiale sortante
            float outSpeed = Vector3.Dot(velocity, dir);
            if (outSpeed > 0f) velocity -= dir * outSpeed;

            // colle un peu vers l'intérieur (tension)
            velocity += dir * grapplePull * Time.deltaTime;
            // amorti léger pour stabiliser
            velocity *= (1f - ropeDamping * Time.deltaTime);
        }
        else
        {
            // si on est plus court que la corde (on s'est rapproché), juste tirer un peu
            velocity += dir * grapplePull * 0.5f * Time.deltaTime;
        }

        // Option de release en saut pour boost naturel (géré par la conservation de vitesse)
    }

    Vector3 GetHip() => transform.position + Vector3.up * (cc.height * 0.5f);

    // -------- Décharge d'énergie (rocket / redirect) --------
    void EnergyBlast()
    {
        // Raycast devant : si proche surface -> impulse selon normale, sinon impulse opposée au regard
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, blastProbe, worldMask, QueryTriggerInteraction.Ignore))
        {
            AddImpulse(hit.normal * blastImpulse);
        }
        else
        {
            AddImpulse(-cam.transform.forward * blastImpulse);
        }
    }

    void AddImpulse(Vector3 impulse)
    {
        velocity += impulse;
    }

    // -------- Stamina / Ressources --------
    void ResetGroundResources()
    {
        dashLeft = maxDash;
        airJumpsLeft = maxAirJumps;
    }

    void RegenStamina(bool grounded)
    {
        float r = grounded ? staminaRegenGround : staminaRegenAir;
        stamina = Mathf.Clamp01(stamina + r * Time.deltaTime);
    }

    // -------- HUD (OnGUI simple) --------
    void OnGUI()
    {
        const int pad = 10;
        const int w = 220;
        const int h = 20;
        int y = pad;

        // Stamina bar
        GUI.Box(new Rect(pad, y, w, h), "Stamina");
        GUI.Box(new Rect(pad + 2, y + 2, (w - 4) * stamina, h - 4), "");
        y += h + 6;

        // Dash / Jumps
        GUI.Label(new Rect(pad, y, w, h), $"Dash: {dashLeft}/{maxDash}   Jumps: {Mathf.Clamp(maxAirJumps - airJumpsLeft, 0, maxAirJumps)}/{maxAirJumps}");
        y += h + 6;

        // Speed
        GUI.Label(new Rect(pad, y, w, h), $"Speed: {speedDisplay:0.0} m/s");
        y += h + 6;

        // Grapple indicator
        if (isGrappling) GUI.Label(new Rect(pad, y, w, h), "Grapple: LATCHED");
    }

    // -------- Gizmos (ligne grappin) --------
    void OnDrawGizmosSelected()
    {
        if (isGrappling)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(GetHip(), grapplePoint);
            Gizmos.DrawWireSphere(grapplePoint, 0.2f);
        }
    }
}
