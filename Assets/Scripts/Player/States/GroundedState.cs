using UnityEngine;

public class GroundedState : IState
{
    readonly Player p;

    public GroundedState(Player player) { p = player; }

    public void Enter()
    {
        p.ignoreGravity = false;
        p.jumpCount = 0;
    }

    public void Tick(float dt)
    {
        if (!p.isGrounded) { p.FSM.ChangeState(new JumpState(p)); return; }

        if (p.input)
        {
            // Slide au sol
            if (p.input.SlidePressed && p.CanStartSlide()) { p.FSM.ChangeState(new SlideState(p)); return; }

            // Dash
            if (p.input.DashPressed && p.CanDash()) { p.FSM.ChangeState(new DashState(p)); return; }

            // Jump (instant + buffer)
            bool wantJump = p.input.JumpPressed || p.pjumpBuffered();
            if (wantJump && p.CanJumpFromGround())
            {
                p.DoJump();
                p.FSM.ChangeState(new JumpState(p));
                return;
            }
        }

        // Move sol
        Vector3 wish = p.WishDir();
        float wishMag = Mathf.Clamp01(wish.magnitude);
        Vector3 wishN = wishMag > 0f ? wish / wishMag : Vector3.zero;

        Vector3 planar = new Vector3(p.velocity.x, 0f, p.velocity.z);
        float targetSpeed = p.data.maxGroundSpeed * wishMag;
        Vector3 targetVel = wishN * targetSpeed;

        Vector3 delta = targetVel - planar;
        float accel = (targetSpeed > planar.magnitude) ? p.data.groundAccel : p.data.groundDecel;
        planar += Vector3.ClampMagnitude(delta, accel * dt);

        p.velocity.x = planar.x;
        p.velocity.z = planar.z;

        if (p.velocity.y > -2f) p.velocity.y = -2f;
    }

    public void Exit() { }
}

static class PlayerJumpBufferExt
{
    public static bool pjumpBuffered(this Player p) => p.jumpBufferTimer > 0f;
}
