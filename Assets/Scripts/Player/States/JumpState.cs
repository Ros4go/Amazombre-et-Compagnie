using UnityEngine;

public class JumpState : IState
{
    readonly Player p;

    public JumpState(Player player) { p = player; }

    public void Enter()
    {
        p.ignoreGravity = false;
    }

    public void Tick(float dt)
    {
        if (p.input)
        {
            // Dash en l'air
            if (p.input.DashPressed && p.CanDash()) { p.FSM.ChangeState(new DashState(p)); return; }

            // Slam = Slide appuyé en l'air
            if (p.input.SlidePressed && !p.isGrounded) { p.FSM.ChangeState(new SlamState(p)); return; }

            // Double jump / coyote
            if ((p.input.JumpPressed || p.pjumpBuffered()) && p.CanJumpInAir())
            {
                p.DoJump();
            }
        }

        // Air control
        Vector3 wish = p.WishDir();
        float wishMag = Mathf.Clamp01(wish.magnitude);
        Vector3 wishN = wishMag > 0f ? wish / wishMag : Vector3.zero;

        Vector3 planar = new Vector3(p.velocity.x, 0f, p.velocity.z);
        float targetSpeed = p.data.maxAirSpeed * wishMag;
        Vector3 targetVel = wishN * targetSpeed;

        Vector3 delta = targetVel - planar;
        planar += Vector3.ClampMagnitude(delta, p.data.airAccel * dt);

        p.velocity.x = planar.x;
        p.velocity.z = planar.z;

        if (p.isGrounded) { p.FSM.ChangeState(new GroundedState(p)); return; }
    }

    public void Exit() { }
}
