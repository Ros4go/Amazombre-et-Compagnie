// Lightweight state machine wrapper so you can wire Mixamo animations later.
// For now, logic mostly lives in PlayerMotor; state machine just exposes state for animation and future expansion.
using UnityEngine;

namespace Amazombre.Player.StateMachine
{
    public enum LocomotionState
    {
        Idle,
        Run,
        Air,
        Dash,
        Slide,
        WallRun
    }

    public class PlayerStateMachine : MonoBehaviour
    {
        public Movement.PlayerMotor motor;
        public Animator animator; // optional (for Mixamo later)

        [Header("Animator Parameters (optional)")]
        public string SpeedParam = "Speed";
        public string GroundedParam = "Grounded";
        public string DashParam = "Dash";
        public string SlideParam = "Slide";
        public string WallRunParam = "WallRun";
        public string YVelParam = "YVelocity";

        public LocomotionState CurrentState { get; private set; }

        void Update()
        {
            if (!motor) return;

            // Derive a simple state for now
            if (motor.IsDashing) CurrentState = LocomotionState.Dash;
            else if (motor.IsSliding) CurrentState = LocomotionState.Slide;
            else if (motor.IsWallRunning) CurrentState = LocomotionState.WallRun;
            else if (!motor.IsGrounded) CurrentState = LocomotionState.Air;
            else if (new Vector3(motor.Velocity.x, 0, motor.Velocity.z).sqrMagnitude > 0.1f) CurrentState = LocomotionState.Run;
            else CurrentState = LocomotionState.Idle;

            if (animator)
            {
                animator.SetFloat(SpeedParam, new Vector3(motor.Velocity.x, 0, motor.Velocity.z).magnitude);
                animator.SetBool(GroundedParam, motor.IsGrounded);
                animator.SetBool(DashParam, motor.IsDashing);
                animator.SetBool(SlideParam, motor.IsSliding);
                animator.SetBool(WallRunParam, motor.IsWallRunning);
                animator.SetFloat(YVelParam, motor.Velocity.y);
            }
        }
    }
}
