using UnityEngine;

public class AirState : IState
{
    private readonly Player p;

    public AirState(Player player) { p = player; }

    public void Enter() { }

    public void Tick()
    {
        float dt = Time.deltaTime;

        // Timers air
        p.coyoteTimer += dt;
        p.timeInAir += dt;

        // Jump en l'air
        if (p.input.ConsumeJumpPressed())
        {
            // 1) Walljump prioritaire si un mur est à portée
            if (TryFindWall(out Vector3 wallNormal))
            {
                DoWallJump(wallNormal);
                return;
            }

            // 2) Sinon: coyote ou multi-saut
            if (p.jumpCount == 0 && p.coyoteTimer <= p.data.coyoteTime)
            {
                ApplyHorizontalJumpBonus();
                p.DoJumpFromGround();
            }
            else if (p.jumpCount > 0 && p.jumpCount < p.data.maxJumps)
            {
                ApplyHorizontalJumpBonus();
                p.DoJumpInAir();
            }
            else
            {
                p.PushJumpBuffer();
            }
        }
        else
        {
            p.DecayJumpBuffer(dt);
        }

        // Air control
        Vector3 wish = p.GetWishDirectionOnPlane();
        p.ApplyAirAcceleration(wish, dt);

        // Auto-WallRun: proche d'un mur + vitesse suffisante + soit approche, soit mouvement parallèle au mur
        if (TryFindWall(out Vector3 wallN))
        {
            Vector3 horizVel = Vector3.ProjectOnPlane(p.velocity, Vector3.up);
            float speed = horizVel.magnitude;
            float approach = (speed > 0.0001f) ? Vector3.Dot(horizVel.normalized, -wallN) : 0f;

            // composante parallèle au plan du mur
            Vector3 along = Vector3.ProjectOnPlane(horizVel, wallN);
            float parallelRatio = (speed > 0.0001f) ? (along.magnitude / speed) : 0f;

            if (speed >= p.data.wallMinSpeed &&
                (approach >= p.data.wallApproachMinDot || parallelRatio >= p.data.wallParallelMinRatio))
            {
                p.FSM.ChangeState(new WallRunState(p, wallN));
                return;
            }
        }

        // Atterrissage walkable: appliquer conservation/perte de vitesse
        if (p.isGrounded && !p.IsTooSteep(p.groundNormal))
        {
            ApplyLandingKeep();
            p.coyoteTimer = 0f;
            p.timeInAir = 0f;
            p.FSM.ChangeState(new GroundedState(p));
        }
    }

    public void Exit() { }

    // ----- Utilitaires -----
    void ApplyHorizontalJumpBonus()
    {
        float horiz = p.HorizontalSpeed;
        float t = Mathf.Clamp01(horiz / Mathf.Max(0.01f, p.data.maxGroundSpeed));
        float bonus = Mathf.Lerp(0f, p.data.jumpHorizBonusMax, t);
        Vector3 h = Vector3.ProjectOnPlane(p.velocity, Vector3.up);
        p.velocity = h * (1f + bonus) + Vector3.up * p.velocity.y;
    }

    void ApplyLandingKeep()
    {
        Vector3 v = p.velocity;
        if (v.sqrMagnitude < 1e-6f) return;

        Vector3 n = p.groundNormal;
        float normalFrac = Mathf.Abs(Vector3.Dot(v.normalized, n));
        float keep = Mathf.Lerp(p.data.landingBadKeep, p.data.landingGoodKeep, 1f - normalFrac);

        Vector3 t = Vector3.ProjectOnPlane(v, n).normalized;
        Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, n).normalized;
        float bonus = Mathf.Max(0f, Vector3.Dot(t, downhill)) * p.data.landingDownhillBonus;
        keep = Mathf.Clamp(keep + bonus, p.data.landingBadKeep, 0.95f);

        float horiz = Vector3.ProjectOnPlane(v, Vector3.up).magnitude;
        Vector3 tangent = Vector3.ProjectOnPlane(v, n).normalized;
        Vector3 newHoriz = tangent * (horiz * keep);

        p.velocity = newHoriz + Vector3.up * p.velocity.y;
    }

    // Walljump hors wallrun: direction opposée au mur + biais vertical
    void DoWallJump(Vector3 wallNormal)
    {
        float v0 = Mathf.Sqrt(2f * p.data.gravityBase * p.data.jumpHeight);

        Vector3 away = wallNormal;     // s'éloigner du mur
        Vector3 up = Vector3.up;
        Vector3 dir = (away * p.data.wallJumpOutward + up * p.data.wallJumpUpward).normalized;

        // Vitesse horizontale cible
        float currentHoriz = Vector3.ProjectOnPlane(p.velocity, Vector3.up).magnitude;
        float targetHoriz = Mathf.Max(p.data.wallJumpMinHorizSpeed, currentHoriz * p.data.wallJumpHorizScale);

        Vector3 horizDir = Vector3.ProjectOnPlane(dir, Vector3.up);
        if (horizDir.sqrMagnitude < 1e-6f) horizDir = Vector3.ProjectOnPlane(-wallNormal, Vector3.up);
        horizDir.Normalize();

        Vector3 newHoriz = horizDir * targetHoriz + wallNormal * p.data.wallJumpSeparationBoost;
        p.SetVelocity(newHoriz + Vector3.up * v0);

        // rester en Air
    }

    // Mur proche (face, droite, gauche)
    bool TryFindWall(out Vector3 wallNormal)
    {
        wallNormal = Vector3.zero;

        Vector3 ccCenter = p.transform.TransformPoint(p.Controller.center);
        float chest = Mathf.Max(0.2f, p.Controller.height * 0.35f);
        Vector3 origin = ccCenter + Vector3.up * chest;

        Vector3 fwd = Vector3.ProjectOnPlane(p.transform.forward, Vector3.up).normalized;
        Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);

        Vector3[] dirs = new[] { fwd, right, -right };
        float bestDot = -1f;
        Vector3 bestNormal = Vector3.zero;

        foreach (var d in dirs)
        {
            if (Physics.Raycast(origin, d, out var hit, p.data.wallCheckDistance, p.data.groundMask, QueryTriggerInteraction.Ignore))
            {
                float dot = Vector3.Dot(-hit.normal, d); // face le mur
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestNormal = hit.normal;
                }
            }
        }

        if (bestDot > 0f)
        {
            wallNormal = bestNormal;
            return true;
        }
        return false;
    }
}
