using UnityEngine;

public class GroundedState : IState
{
    private readonly Player p;

    public GroundedState(Player player) { p = player; }

    public void Enter()
    {
        p.gravityScale = 1f;
        p.timeInAir = 0f;
        p.coyoteTimer = 0f;
        if (p.jumpCount > 0) p.jumpCount = 0;
    }

    public void Tick()
    {
        float dt = Time.deltaTime;

        // Jump buffer (si appui avant l'atterrissage)
        if (p.input.ConsumeJumpPressed())
            p.PushJumpBuffer();
        else
            p.DecayJumpBuffer(dt);

        // Mouvement au sol
        Vector3 wish = p.GetWishDirectionOnPlane();
        if (wish.sqrMagnitude < 1e-6f)
            p.ApplyGroundFriction(dt);
        else
            p.ApplyGroundAcceleration(wish, dt);

        // Saut sol (pente walkable uniquement)
        if (p.HasJumpBuffer && p.CanGroundJump())
        {
            ApplyHorizontalJumpBonus();
            p.DoJumpFromGround();
            p.ClearJumpBuffer();
            p.FSM.ChangeState(new AirState(p));
            return;
        }

        // Sortie si on n’est plus au sol
        if (!p.isGrounded)
        {
            p.FSM.ChangeState(new AirState(p));
        }
    }

    public void Exit() { }

    // Petit bonus horizontal dépendant de la vitesse (sauts longs)
    void ApplyHorizontalJumpBonus()
    {
        float horiz = p.HorizontalSpeed;
        float t = Mathf.Clamp01(horiz / Mathf.Max(0.01f, p.data.maxGroundSpeed));
        float bonus = Mathf.Lerp(0f, p.data.jumpHorizBonusMax, t); // ex: jusqu’à +8 %
        Vector3 h = Vector3.ProjectOnPlane(p.velocity, Vector3.up);
        p.velocity = h * (1f + bonus) + Vector3.up * p.velocity.y;
    }
}
