using UnityEngine;

public class SlideState : IState
{
    readonly Player p;
    Vector3 dirPlanar;
    CharacterController cc;
    Vector3 lastPos;

    public SlideState(Player player) { p = player; }

    public void Enter()
    {
        p.ignoreGravity = false;
        p.slideCooldownTimer = p.data.slideCooldown;
        p.SetCrouch(true); // petite hitbox dès l'entrée

        if (!cc) cc = p.GetComponent<CharacterController>();

        // Direction initiale : input fort sinon regard
        Vector2 m = p.input ? p.input.Move : Vector2.zero;
        bool strong = m.sqrMagnitude >= (p.data.slideInputThreshold * p.data.slideInputThreshold);
        if (strong) { dirPlanar = p.WishDir(); dirPlanar.y = 0f; }
        else
        {
            Vector3 look = p.cameraPivot ? p.cameraPivot.forward : p.transform.forward;
            look.y = 0f; dirPlanar = (look.sqrMagnitude > 0f ? look.normalized : p.transform.forward);
        }
        if (dirPlanar.sqrMagnitude < 1e-4f) dirPlanar = p.transform.forward;
        dirPlanar.y = 0f; dirPlanar.Normalize();

        // Vitesse constante
        Vector3 planar = dirPlanar * p.data.slideSpeed;
        p.velocity.x = planar.x; p.velocity.z = planar.z;
        p.velocity.y = -Mathf.Max(2f, p.data.slideStickDownVel);

        lastPos = p.transform.position;
    }

    public void Tick(float dt)
    {
        // 1) Pas de slide en l'air : on sort immédiatement vers le saut
        if (!p.isGrounded)
        {
            p.FSM.ChangeState(new JumpState(p));
            return;
        }

        // 2) Si on n'avance plus dans la direction du slide -> stop auto (mur)
        //    On mesure l'avancement réel depuis la frame précédente
        Vector3 delta = p.transform.position - lastPos;
        float forwardProgress = Vector3.Dot(delta, dirPlanar);
        if (forwardProgress <= 0.001f) // seuil simple
        {
            p.velocity.x = 0f; p.velocity.z = 0f; // couper le slide
            p.FSM.ChangeState(new CrouchState(p));
            return;
        }

        // --- existant ---
        p.EnsureCrouched();

        if (p.input != null && p.input.DashPressed && p.CanDash()) { p.FSM.ChangeState(new DashState(p)); return; }

        p.velocity.y = -Mathf.Max(2f, p.data.slideStickDownVel);
        TrySnapDown();

        if (p.input == null || !p.input.SlideHeld)
        {
            p.velocity.x = 0f; p.velocity.z = 0f;
            p.FSM.ChangeState(new CrouchState(p));
            return;
        }

        Vector3 desired = dirPlanar;
        if (p.input != null)
        {
            Vector3 wish = p.WishDir(); wish.y = 0f;
            if (wish.sqrMagnitude > 1e-4f) desired = wish.normalized;
        }
        float maxRad = Mathf.Deg2Rad * Mathf.Max(0f, p.data.slideTurnRateDeg);
        dirPlanar = Vector3.RotateTowards(dirPlanar, desired, maxRad * dt, 0f).normalized;

        Vector3 newPlanar = dirPlanar * p.data.slideSpeed;
        p.velocity.x = newPlanar.x; p.velocity.z = newPlanar.z;

        // --- Nouveau : maj pour la prochaine frame ---
        lastPos = p.transform.position;
    }

    public void Exit() { }

    void TrySnapDown()
    {
        if (!cc) return;
        float probe = Mathf.Max(0.05f, p.data.slideSnapProbe);
        Vector3 origin = p.transform.position + Vector3.up * (cc.radius + 0.05f);
        float radius = cc.radius * 0.95f;
        Physics.SphereCast(origin, radius, Vector3.down, out _, probe, ~0, QueryTriggerInteraction.Ignore);
    }
}
