using UnityEngine;

[CreateAssetMenu(fileName = "MovementData", menuName = "Amazombre/MovementDataSO")]
public class MovementDataSO : ScriptableObject
{
    // --------- SOL (mouvement au sol) ---------
    [Header("Ground")]
    [Tooltip("Vitesse horizontale maximale sur un sol marchable (walkable).")]
    [Min(0)] public float maxGroundSpeed = 12f;

    [Tooltip("Acc�l�ration horizontale appliqu�e au sol quand il y a de l'input.")]
    [Min(0)] public float accelGround = 80f;

    [Tooltip("Freinage au sol quand il n'y a PAS d'input (ram�ne la vitesse � 0).")]
    [Min(0)] public float decelGround = 90f;

    // --------- AIR (contr�le en l'air) ---------
    [Header("Air")]
    [Tooltip("Vitesse horizontale maximale quand le joueur est en l'air.")]
    [Min(0)] public float maxAirSpeed = 14f;

    [Tooltip("Acc�l�ration horizontale en l'air (air control).")]
    [Min(0)] public float accelAir = 50f;

    [Tooltip("Facteur d'air-control pendant la mont�e (0 = aucun contr�le, 1 = contr�le total).")]
    [Range(0f, 1f)] public float airControlAscendFactor = 0.6f;

    [Tooltip("Facteur d'air-control pendant la descente (>= au facteur de mont�e).")]
    [Range(0f, 1.5f)] public float airControlDescendFactor = 1.0f;

    // --------- GRAVIT� & SAUT ---------
    [Header("Gravity / Jump")]
    [Tooltip("Gravit� appliqu�e au d�but de la chute ou juste apr�s un saut.")]
    [Min(0)] public float gravityBase = 25f;

    [Tooltip("Gravit� maximale atteinte apr�s un certain temps en l'air.")]
    [Min(0)] public float gravityMax = 60f;

    [Tooltip("Temps (secondes) pour passer de gravityBase � gravityMax.")]
    [Min(0.01f)] public float gravityRampTime = 0.8f;

    [Tooltip("Vitesse de chute maximale (clamp de la vitesse verticale).")]
    [Min(0)] public float terminalVelocity = 75f;

    [Tooltip("Hauteur cible d'un saut (chaque impulsion pose velY pour atteindre ~cette hauteur avec gravityBase).")]
    [Min(0)] public float jumpHeight = 2.2f;

    [Tooltip("Nombre total de sauts possibles (1 = simple, 2 = double, etc.).")]
    [Min(1)] public int maxJumps = 2;

    [Tooltip("Fen�tre pour sauter apr�s avoir quitt� le sol (coyote time).")]
    [Range(0f, 0.5f)] public float coyoteTime = 0.12f;

    [Tooltip("Fen�tre pour enregistrer un appui saut AVANT d'atterrir (jump buffer).")]
    [Range(0f, 0.5f)] public float jumpBuffer = 0.15f;

    // --------- D�TECTION DE SOL ---------
    [Header("Grounding")]
    [Tooltip("Marge ajout�e sous la capsule pour les v�rifications de sol.")]
    [Range(0f, 0.5f)] public float groundCheckExtra = 0.08f;

    [Tooltip("Couches consid�r�es comme 'sol'.")]
    public LayerMask groundMask = ~0;

    // --------- PENTES / GLISSE ---------
    [Header("Slope / Slide")]
    [Tooltip("Seuil d'angle (degr�s) au-del� duquel on consid�re la pente trop raide et on d�clenche la glisse.")]
    [Range(0f, 89.9f)] public float slopeSlideThresholdDeg = 45f;

    [Tooltip("Acc�l�ration de base appliqu�e pendant la glisse sur pente.")]
    [Min(0)] public float slopeSlideBaseAccel = 8f;

    [Tooltip("Bonus d'acc�l�ration qui augmente avec le temps pass� � glisser (par seconde).")]
    [Min(0)] public float slopeSlideAccelPerSec = 12f;

    [Tooltip("Vitesse horizontale maximale atteignable pendant la glisse.")]
    [Min(0)] public float slopeSlideMaxSpeed = 30f;

    [Tooltip("Poids du terme gravitationnel projet� sur la pente (g�sin0).")]
    [Min(0)] public float slopeGravityFactor = 1.0f;

    [Tooltip("Courbure de l'influence de l'angle de la pente (1 = lin�aire, >1 = favorise les pentes raides).")]
    [Min(0.1f)] public float slopeAnglePower = 1.2f;

    // --------- RALENTISSEMENT EN MONT�E (sol walkable) ---------
    [Header("Uphill (ralentissement)")]
    [Tooltip("Coefficient sur la VITESSE cible en mont�e (1 = aucune p�nalit�).")]
    [Range(0.1f, 1f)] public float uphillSpeedScale = 0.75f;

    [Tooltip("Coefficient sur l'ACC�L�RATION en mont�e (1 = aucune p�nalit�).")]
    [Range(0.1f, 1f)] public float uphillAccelScale = 0.6f;

    // ----- Landing (conservation de vitesse) -----
    [Header("Landing Keep")]
    [Tooltip("Facteur de conservation de vitesse si l�atterrissage est bien align� (0..1).")]
    [Range(0f, 1f)] public float landingGoodKeep = 0.90f;

    [Tooltip("Facteur si l�angle d�impact est mauvais (0..1).")]
    [Range(0f, 1f)] public float landingBadKeep = 0.35f;

    [Tooltip("Bonus ajout� si on pose dans le sens de la pente (0..0.2 recommand�).")]
    [Range(0f, 0.2f)] public float landingDownhillBonus = 0.10f;

    // ----- Jump bonus horizontal -----
    [Header("Jump Bonus")]
    [Tooltip("Bonus horizontal max appliqu� au moment du saut, proportionnel � la vitesse (ex: 0.08 = +8%).")]
    [Range(0f, 0.2f)] public float jumpHorizBonusMax = 0.08f;

    // ----- Wall Run -----
    [Header("Wall Run")]
    [Tooltip("Acc�l�ration le long du mur pendant le wall-run (m/s�).")]
    [Min(0f)] public float wallRunAccel = 4f;

    [Tooltip("Facteur de gravit� pendant le wall-run (1 = normal, 0.4 conseill�).")]
    [Range(0f, 1f)] public float wallRunGravityScale = 0.4f;

    [Tooltip("Dur�e max d�un wall-run (secondes).")]
    [Min(0f)] public float wallRunMaxTime = 1.0f;

    [Tooltip("Distance de d�tection du mur (m).")]
    [Range(0.1f, 2f)] public float wallCheckDistance = 0.8f;

    [Tooltip("Seuil de face-�-face avec le mur (dot avec la vitesse), 0.3 = 72� max.")]
    [Range(-1f, 1f)] public float wallApproachMinDot = 0.3f;

    [Tooltip("Seuil d�input vers le mur (dot entre l�input et -normal), 0.2 conseill�.")]
    [Range(0f, 1f)] public float wallInputTowardsMinDot = 0.2f;

    [Tooltip("Vitesse horizontale minimale pour engager un wall-run.")]
    [Min(0f)] public float wallMinSpeed = 3f;

    // ----- Wall Jump -----
    [Header("Wall Jump")]
    [Tooltip("Poids de la composante 's'�loigner du mur' (direction normale).")]
    [Range(0f, 2f)] public float wallJumpOutward = 1.0f;

    [Tooltip("Poids de la composante verticale du saut de mur.")]
    [Range(0f, 2f)] public float wallJumpUpward = 0.65f;

    [Tooltip("Vitesse horizontale minimale vis�e au d�part du wall jump.")]
    [Min(0f)] public float wallJumpMinHorizSpeed = 6f;

    [Tooltip("Multiplicateur de la vitesse horizontale actuelle pour calculer la vitesse vis�e.")]
    [Range(0f, 2f)] public float wallJumpHorizScale = 1.0f;

    [Tooltip("Petit boost d'�loignement imm�diat pour d�coller du mur (m/s).")]
    [Min(0f)] public float wallJumpSeparationBoost = 2f;

    // ----- Wall Chain (re-grab rapide gauche/droite) -----
    [Header("Wall Chain")]
    [Tooltip("Fen�tre (s) apr�s un wall-run/jump pendant laquelle on peut re-grabber le mur oppos� avec des conditions all�g�es.")]
    [Min(0f)] public float wallRegrabGrace = 0.20f;

    [Tooltip("Pendant la fen�tre, seuil d'approche assoupli (dot entre vitesse et -normal du mur). 0.1 = 84� max.")]
    [Range(-1f, 1f)] public float wallChainApproachDot = 0.10f;

    [Tooltip("Temps mini (s) avant de pouvoir re-grabber exactement le m�me mur pour �viter l'effet ventouse.")]
    [Min(0f)] public float wallSameSideCooldown = 0.15f;

    [Tooltip("Auto-wallrun: part si le mouvement est assez parall�le au mur. 0.6 = 60% de la vitesse align�e au plan du mur.")]
    [Range(0f, 1f)] public float wallParallelMinRatio = 0.6f;


    // --------- Validation automatique ---------
    void OnValidate()
    {
        gravityMax = Mathf.Max(gravityMax, gravityBase);
        airControlDescendFactor = Mathf.Max(airControlDescendFactor, airControlAscendFactor);
        slopeAnglePower = Mathf.Max(0.1f, slopeAnglePower);

        maxJumps = Mathf.Max(1, maxJumps);
        coyoteTime = Mathf.Clamp(coyoteTime, 0f, 0.5f);
        jumpBuffer = Mathf.Clamp(jumpBuffer, 0f, 0.5f);
        groundCheckExtra = Mathf.Clamp(groundCheckExtra, 0f, 0.5f);
    }
}
