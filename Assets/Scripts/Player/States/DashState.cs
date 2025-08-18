using UnityEngine;

public class DashState : IState
{
    readonly Player p;
    float timer;
    Vector3 dashDir;

    public DashState(Player player) { p = player; }

    public void Enter()
    {
        timer = 0f;
        p.ConsumeDashCharge();

        dashDir = p.cameraPivot ? p.cameraPivot.forward : p.transform.forward;
        dashDir = Vector3.ClampMagnitude(new Vector3(dashDir.x, dashDir.y * 0.5f, dashDir.z), 1f);
        dashDir.Normalize();

        p.ignoreGravity = true;

        p.velocity = Vector3.ProjectOnPlane(p.velocity, Vector3.up);
        p.velocity = dashDir * p.data.dashSpeed;
    }

    public void Tick(float dt)
    {
        timer += dt;

        p.velocity = dashDir * p.data.dashSpeed;

        if (p.input)
        {
            // Slam = slide en l'air
            if (!p.isGrounded && p.input.SlidePressed) { p.FSM.ChangeState(new SlamState(p)); return; }
            // Slide-cancel au sol
            if (p.isGrounded && p.input.SlidePressed && p.CanStartSlide()) { p.FSM.ChangeState(new SlideState(p)); return; }
        }

        if (timer >= p.data.dashDuration)
        {
            p.ignoreGravity = false;
            if (p.isGrounded) p.FSM.ChangeState(new GroundedState(p));
            else p.FSM.ChangeState(new JumpState(p));
        }
    }

    public void Exit() { p.ignoreGravity = false; }
}
