using UnityEngine;

namespace Amazombre.Player.Movement
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        public CharacterController controller { get; private set; }

        [Header("Settings")]
        public float gravity = -30f;
        public float groundedGravity = -5f;
        public float maxSpeed = 14f;
        public float sprintSpeed = 18f;
        public float acceleration = 80f;
        public float airAcceleration = 12f;
        public float friction = 12f;
        public float jumpHeight = 1.3f;
        public float coyoteTime = 0.12f;
        public float jumpBuffer = 0.12f;

        [Header("Advanced Movement")]
        public float dashSpeed = 30f;
        public float dashDuration = 0.18f;
        public float dashCooldown = 0.65f;
        public float slideSpeed = 16f;
        public float slideFriction = 2.5f;
        public float slideDuration = 0.75f;
        public float wallRunSpeed = 14f;
        public float wallRunGravity = -6f;
        public float wallJumpImpulse = 10f;
        public float wallCheckDistance = 0.8f;
        public LayerMask wallMask;

        [Header("References")]
        public Core.PlayerInputReader input;

        // ---- State ----
        private Vector3 _velocity;
        public Vector3 Velocity => _velocity;

        public bool IsGrounded => controller && controller.isGrounded;
        public bool IsDashing { get; private set; }
        public bool IsSliding { get; private set; }
        public bool IsWallRunning { get; private set; }
        public Vector3 WallNormal { get; private set; }

        float coyoteTimer, jumpBufferTimer, dashTimer, dashCDTimer, slideTimer;
        Vector3 dashDir;

        void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (!controller || !input) return;

            float dt = Time.deltaTime;

            // --- Ground handling
            if (IsGrounded && _velocity.y < 0f)
            {
                _velocity.y = groundedGravity;
            }

            // Coyote / jump buffer
            coyoteTimer = IsGrounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - dt);
            jumpBufferTimer = input.JumpPressed ? jumpBuffer : Mathf.Max(0f, jumpBufferTimer - dt);

            // --- Dash
            if (dashCDTimer > 0f) dashCDTimer -= dt;
            if (!IsDashing && dashCDTimer <= 0f && input.DashPressed)
            {
                IsDashing = true;
                dashTimer = dashDuration;
                dashCDTimer = dashCooldown;

                var move = input.MoveAxis;
                var forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                var right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
                var wish = (forward * move.y + right * move.x);
                dashDir = wish.sqrMagnitude > 0.01f ? wish.normalized : forward;
            }

            // --- Slide
            if (input.SlideHeld && IsGrounded && !IsSliding && controller.velocity.magnitude > maxSpeed * 0.6f)
            {
                IsSliding = true;
                slideTimer = slideDuration;
            }

            // --- Wallrun detect
            if (!IsGrounded && !IsDashing)
            {
                if (CheckWall(out var n))
                {
                    var forward = transform.forward;
                    var alongWall = Vector3.Cross(n, Vector3.up);
                    if (Vector3.Dot(forward, alongWall) > 0.2f)
                    {
                        IsWallRunning = true;
                        WallNormal = n;
                    }
                }
                else IsWallRunning = false;
            }
            else IsWallRunning = false;

            // --- Input wish dir
            var moveAxis = input.MoveAxis;
            var camF = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            var camR = new Vector3(transform.right.x, 0, transform.right.z).normalized;
            var wishDir = (camF * moveAxis.y + camR * moveAxis.x);
            wishDir = wishDir.sqrMagnitude > 0.001f ? wishDir.normalized : Vector3.zero;
            float targetSpeed = input.SprintHeld ? sprintSpeed : maxSpeed;

            // --- Apply per-state movement
            if (IsDashing)
            {
                _velocity = dashDir * dashSpeed;
                _velocity.y = 0f; // flat dash
                dashTimer -= dt;
                if (dashTimer <= 0f) IsDashing = false;
            }
            else if (IsSliding)
            {
                Vector3 lateral = new Vector3(_velocity.x, 0f, _velocity.z);
                lateral = Vector3.MoveTowards(lateral, wishDir * slideSpeed, slideFriction * dt);
                _velocity.x = lateral.x;
                _velocity.z = lateral.z;
                _velocity.y += gravity * dt;

                slideTimer -= dt;
                if (slideTimer <= 0f || !input.SlideHeld) IsSliding = false;
            }
            else if (IsWallRunning)
            {
                var along = Vector3.Cross(WallNormal, Vector3.up).normalized;
                float side = Vector3.Dot(along, camF) >= 0 ? 1f : -1f;
                Vector3 alongDir = along * side;
                var target = alongDir * wallRunSpeed;

                Vector3 lateral = new Vector3(_velocity.x, 0f, _velocity.z);
                lateral = Vector3.MoveTowards(lateral, target, acceleration * dt);
                _velocity.x = lateral.x;
                _velocity.z = lateral.z;

                _velocity.y = Mathf.MoveTowards(_velocity.y, wallRunGravity, 60f * dt);

                // Wall jump (uses jump buffer)
                if (jumpBufferTimer > 0f)
                {
                    jumpBufferTimer = 0f;
                    var jump = (-WallNormal + Vector3.up).normalized * wallJumpImpulse;
                    _velocity.x = jump.x * targetSpeed;
                    _velocity.z = jump.z * targetSpeed;
                    _velocity.y = Mathf.Sqrt(2f * -gravity * jumpHeight);
                    IsWallRunning = false;
                    coyoteTimer = 0f;
                }
            }
            else
            {
                Vector3 lateral = new Vector3(_velocity.x, 0f, _velocity.z);
                float accel = IsGrounded ? acceleration : airAcceleration;
                Vector3 target = wishDir * targetSpeed;
                lateral = Vector3.MoveTowards(lateral, target, accel * dt);
                _velocity.x = lateral.x;
                _velocity.z = lateral.z;

                // Jump
                if (jumpBufferTimer > 0f && coyoteTimer > 0f)
                {
                    jumpBufferTimer = 0f;
                    coyoteTimer = 0f;
                    _velocity.y = Mathf.Sqrt(2f * -gravity * jumpHeight);
                }

                // Gravity
                _velocity.y += gravity * dt;
            }

            controller.Move(_velocity * dt);
        }

        bool CheckWall(out Vector3 normal)
        {
            normal = Vector3.zero;
            var origin = transform.position + Vector3.up * (controller.height * 0.5f);

            if (Physics.SphereCast(origin, controller.radius * 0.9f, transform.right, out var hit1, wallCheckDistance, wallMask, QueryTriggerInteraction.Ignore))
            { normal = hit1.normal; return true; }

            if (Physics.SphereCast(origin, controller.radius * 0.9f, -transform.right, out var hit2, wallCheckDistance, wallMask, QueryTriggerInteraction.Ignore))
            { normal = hit2.normal; return true; }

            return false;
        }
    }
}
