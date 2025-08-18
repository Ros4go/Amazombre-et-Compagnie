using UnityEngine;

public class CrouchState : IState
{
    readonly Player p;
    float standCheckTimer;

    public CrouchState(Player player) { p = player; }

    public void Enter()
    {
        p.ignoreGravity = false;
        p.SetCrouch(true);
        standCheckTimer = 0f;
    }

    public void Tick(float dt)
    {
        // Sécurité : rester en petite hitbox tant qu'on est dans cet état
        p.EnsureCrouched();

        if (!p.isGrounded) { p.FSM.ChangeState(new JumpState(p)); return; }

        // Dash depuis crouch
        if (p.input != null && p.input.DashPressed && p.CanDash()) { p.FSM.ChangeState(new DashState(p)); return; }

        // Re-slide si on tient la touche et cooldown ok
        if (p.input != null && p.input.SlideHeld && p.CanStartSlide()) { p.FSM.ChangeState(new SlideState(p)); return; }

        // Jump: avec ou sans headroom (duck jump)
        if (p.input != null && p.input.JumpPressed)
        {
            if (p.HasHeadroom()) { p.SetCrouch(false); p.DoJump(); }
            else { p.DuckJump(); } // reste crouch, on se relèvera plus tard
            p.FSM.ChangeState(new JumpState(p));
            return;
        }

        // Déplacement accroupi
        Vector3 wish = p.WishDir();
        float wishMag = Mathf.Clamp01(wish.magnitude);
        Vector3 wishN = wishMag > 0f ? wish / wishMag : Vector3.zero;

        Vector3 planar = new Vector3(p.velocity.x, 0f, p.velocity.z);
        float targetSpeed = p.data.crouchWalkSpeed * wishMag;
        Vector3 targetVel = wishN * targetSpeed;

        Vector3 delta = targetVel - planar;
        float accel = (targetSpeed > planar.magnitude) ? p.data.crouchAccel : p.data.crouchDecel;
        planar += Vector3.ClampMagnitude(delta, accel * dt);

        p.velocity.x = planar.x;
        p.velocity.z = planar.z;
        p.velocity.y = -2f;

        // Auto-relève si on ne tient PAS la touche et qu'il y a de la place
        if ((p.input == null || !p.input.SlideHeld) && p.HasHeadroom())
        {
            standCheckTimer += dt;
            if (standCheckTimer >= p.data.standClearTime)
            {
                p.SetCrouch(false);
                p.FSM.ChangeState(new GroundedState(p));
                return;
            }
        }
        else standCheckTimer = 0f;
    }

    public void Exit() { }
}
