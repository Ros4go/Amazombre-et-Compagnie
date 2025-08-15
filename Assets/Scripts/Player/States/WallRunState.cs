using UnityEngine;

public class WallRunState : IState
{
    private readonly Player p;
    private Vector3 wallNormal;
    private float timer;

    public WallRunState(Player player, Vector3 normal)
    {
        p = player;
        wallNormal = normal.normalized;
    }

    public void Enter()
    {
        timer = 0f;
        p.gravityScale = p.data.wallRunGravityScale;
        p.SetVelocity(Vector3.ProjectOnPlane(p.velocity, wallNormal));
    }

    public void Tick()
    {
        float dt = Time.deltaTime;
        timer += dt;

        // Coller la vélocité au plan du mur
        p.SetVelocity(Vector3.ProjectOnPlane(p.velocity, wallNormal));

        // Accélération le long du mur (optionnelle, suivant l'input ou la vitesse existante)
        Vector3 wish = p.GetWishDirectionOnPlane();
        Vector3 along = Vector3.ProjectOnPlane(wish.sqrMagnitude > 0 ? wish : p.velocity, wallNormal);
        if (along.sqrMagnitude > 1e-6f)
        {
            along.Normalize();
            p.ForceAddVelocity(along * p.data.wallRunAccel * dt);
        }

        // Saut depuis le mur (opposé au mur + biais haut)
        if (p.input.ConsumeJumpPressed())
        {
            float v0 = Mathf.Sqrt(2f * p.data.gravityBase * p.data.jumpHeight);

            Vector3 away = wallNormal;
            Vector3 up = Vector3.up;
            Vector3 dir = (away * p.data.wallJumpOutward + up * p.data.wallJumpUpward).normalized;

            float currentHoriz = Vector3.ProjectOnPlane(p.velocity, Vector3.up).magnitude;
            float targetHoriz = Mathf.Max(p.data.wallJumpMinHorizSpeed, currentHoriz * p.data.wallJumpHorizScale);

            Vector3 horizDir = Vector3.ProjectOnPlane(dir, Vector3.up);
            if (horizDir.sqrMagnitude < 1e-6f) horizDir = Vector3.ProjectOnPlane(-wallNormal, Vector3.up);
            horizDir.Normalize();

            Vector3 newHoriz = horizDir * targetHoriz + wallNormal * p.data.wallJumpSeparationBoost;
            p.SetVelocity(newHoriz + Vector3.up * v0);

            p.gravityScale = 1f;
            p.FSM.ChangeState(new AirState(p));
            return;
        }

        // Quitter le mur si plus de contact ou temps écoulé (pas d'exigence d'input)
        if (!StillHasWall(out var currentNormal) || timer >= p.data.wallRunMaxTime)
        {
            p.gravityScale = 1f;
            p.FSM.ChangeState(new AirState(p));
            return;
        }

        // Mur incurvé
        wallNormal = currentNormal;

        // Sol walkable
        if (p.isGrounded && !p.IsTooSteep(p.groundNormal))
        {
            p.gravityScale = 1f;
            p.FSM.ChangeState(new GroundedState(p));
        }
    }

    public void Exit()
    {
        p.gravityScale = 1f;
    }

    bool StillHasWall(out Vector3 normal)
    {
        normal = wallNormal;
        Vector3 ccCenter = p.transform.TransformPoint(p.Controller.center);
        float chest = Mathf.Max(0.2f, p.Controller.height * 0.35f);
        Vector3 origin = ccCenter + Vector3.up * chest;

        if (Physics.Raycast(origin, -wallNormal, out var hit, p.data.wallCheckDistance * 1.1f, p.data.groundMask, QueryTriggerInteraction.Ignore))
        {
            normal = hit.normal;
            return true;
        }
        return false;
    }
}
