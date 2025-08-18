using UnityEngine;

public class SlamState : IState
{
    readonly Player p;
    float lockTimer;

    public SlamState(Player player) { p = player; }

    public void Enter()
    {
        p.ignoreGravity = false;
        lockTimer = p.data.slamLockTime;
        p.velocity.y = -p.data.slamDownForce;
    }

    public void Tick(float dt)
    {
        if (lockTimer > 0f) lockTimer -= dt;

        // Dash pendant slam autorisé
        if (p.input && p.input.DashPressed && p.CanDash()) { p.FSM.ChangeState(new DashState(p)); return; }

        if (p.isGrounded && lockTimer <= 0f)
        {
            var cam = Object.FindFirstObjectByType<FpsCamera>();
            if (cam && cam.player == p) cam.Kick();
            p.FSM.ChangeState(new GroundedState(p));
            return;
        }
    }

    public void Exit() { }
}
