using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Refs")]
    public InputReader input;
    public MovementDataSO data;
    public Transform cameraPivot;

    [Header("Runtime (read-only)")]
    public Vector3 velocity;
    public bool isGrounded;
    public int jumpCount;
    public float coyoteTimer;
    public float jumpBufferTimer;

    [Header("Dash (runtime)")]
    public int dashCharges;
    float dashRechargeTimer;
    float dashCooldownTimer;
    public bool ignoreGravity;

    [Header("Crouch/Slide")]
    public bool isCrouched;
    public float slideCooldownTimer;

    CharacterController cc;
    public float ccHeight0 { get; private set; }
    public Vector3 ccCenter0 { get; private set; }

    StateMachine fsm = new StateMachine();
    Vector3 planarFwd, planarRight;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        ccHeight0 = cc.height;
        ccCenter0 = cc.center;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        fsm.ChangeState(new GroundedState(this));
        dashCharges = data != null ? data.dashMaxCharges : 1;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        isGrounded = cc.isGrounded;
        if (isGrounded) coyoteTimer = 0f; else coyoteTimer += dt;

        if (jumpBufferTimer > 0f) jumpBufferTimer -= dt;
        if (slideCooldownTimer > 0f) slideCooldownTimer -= dt;

        if (input && input.JumpPressed) jumpBufferTimer = data.jumpBufferTime;

        BuildPlanarBasis();
        fsm.Tick(dt);

        ApplyGravity(dt);

        cc.Move(velocity * dt);

        if (dashCooldownTimer > 0f) dashCooldownTimer -= dt;
        else if (dashCharges < data.dashMaxCharges)
        {
            dashRechargeTimer += dt;
            if (dashRechargeTimer >= data.dashRechargeTime)
            {
                dashRechargeTimer -= data.dashRechargeTime;
                dashCharges = Mathf.Min(dashCharges + 1, data.dashMaxCharges);
            }
        }

        if (isGrounded && velocity.y < 0f) velocity.y = -2f;
    }

    void BuildPlanarBasis()
    {
        Vector3 fwd = cameraPivot ? cameraPivot.forward : transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) fwd = transform.forward;
        planarFwd = fwd.normalized;
        planarRight = new Vector3(planarFwd.z, 0f, -planarFwd.x);
    }

    public void ApplyGravity(float dt)
    {
        if (ignoreGravity) return;
        if (isGrounded) return;
        velocity.y -= data.gravity * dt;
    }

    public void DoJump()
    {
        float v0 = Mathf.Sqrt(2f * data.gravity * data.jumpHeight);
        velocity.y = v0;
        isGrounded = false;
        jumpCount++;
        jumpBufferTimer = 0f;
    }

    public bool CanJumpFromGround() => isGrounded && jumpCount == 0;
    public bool CanJumpInAir() => !isGrounded && jumpCount < data.maxJumps && (jumpCount > 0 || coyoteTimer <= data.coyoteTime);

    public bool CanDash() => dashCharges > 0;
    public void ConsumeDashCharge()
    {
        dashCharges = Mathf.Max(0, dashCharges - 1);
        dashCooldownTimer = data.dashRechargeDelay;
        dashRechargeTimer = 0f;
    }

    public void SetCrouch(bool on)
    {
        if (isCrouched == on) return;
        isCrouched = on;

        float radius = cc.radius;
        Vector3 up = transform.up;

        Vector3 worldCenterBefore = transform.TransformPoint(cc.center);
        Vector3 bottomWorld = worldCenterBefore - up * (cc.height * 0.5f);

        float minHeight = 2f * radius + 0.02f;
        float targetHeight = on ? Mathf.Max(data.crouchHeight, minHeight)
                                : Mathf.Max(ccHeight0, minHeight);

        cc.height = targetHeight;

        Vector3 targetWorldCenter = bottomWorld + up * (targetHeight * 0.5f);
        Vector3 newCenterLocal = transform.InverseTransformPoint(targetWorldCenter);
        cc.center = new Vector3(newCenterLocal.x, newCenterLocal.y, newCenterLocal.z);

        // forcer l’update du CC cette frame
        cc.Move(Vector3.zero);
        // (optionnel) Physics.SyncTransforms();
        // Debug:
        Debug.Log($"[Crouch:{on}] CC.height={cc.height:F2} centerY={cc.center.y:F2}");
    }

    // Force la petite hitbox si on est en crouch (utile si un autre script a modifié height/center)
    public void EnsureCrouched()
    {
        if (!isCrouched) return;

        float radius = cc.radius;
        float minHeight = 2f * radius + 0.02f;
        float expected = Mathf.Max(data.crouchHeight, minHeight);

        // Tolérance pour éviter de re-appliquer en boucle si c'est déjà bon
        if (Mathf.Abs(cc.height - expected) > 0.002f)
        {
            SetCrouch(true);
        }
    }

    public Vector3 WishDir()
    {
        if (!input) return Vector3.zero;
        Vector2 m = input.Move;
        return (planarFwd * m.y + planarRight * m.x);
    }

    public Vector3 SlideWishOrLook()
    {
        Vector3 wish = WishDir();
        if (wish.sqrMagnitude > 0f) return wish.normalized;

        Vector3 fwd = cameraPivot ? cameraPivot.forward : transform.forward;
        fwd.y = 0f;
        return fwd.sqrMagnitude > 0f ? fwd.normalized : Vector3.forward;
    }

    public bool CanStartSlide() => isGrounded && slideCooldownTimer <= 0f;

    // --- HEADROOM FIABLE (ignore ta propre couche) ---
    public bool HasHeadroom(float skin = 0.01f)
    {
        if (!cc) return true;

        float radius = Mathf.Max(0.01f, cc.radius - skin);
        Vector3 up = transform.up;

        // bottom world actuel
        Vector3 worldCenter = transform.TransformPoint(cc.center);
        Vector3 bottomWorld = worldCenter - up * (cc.height * 0.5f - radius);

        // Capsule "debout"
        float standHeight = Mathf.Max(ccHeight0, 2f * radius + 0.01f);
        Vector3 p1 = bottomWorld + up * radius;
        Vector3 p2 = bottomWorld + up * (standHeight - radius);

        int mask = ~(1 << gameObject.layer); // ignore la couche du player
        bool blocked = Physics.CheckCapsule(p1, p2, radius, mask, QueryTriggerInteraction.Ignore);
        return !blocked;
    }

    public void DuckJump()
    {
        DoJump(); // on reste en crouch; on se relèvera plus tard
    }

    public StateMachine FSM => fsm;
}
