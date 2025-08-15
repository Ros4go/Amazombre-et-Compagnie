using UnityEngine;

[CreateAssetMenu(fileName = "MovementData", menuName = "Amazombre/MovementDataSO")]
public class MovementDataSO : ScriptableObject
{
    [Header("Ground")]
    public float maxGroundSpeed = 12f;
    public float accelGround = 80f;
    public float decelGround = 90f;
    public float slopeSlideThresholdDeg = 45f;
    public float slopeSlideBaseAccel = 8f;       // accélération de base sur pente
    public float slopeSlideAccelPerSec = 12f;    // bonus d'accélération qui croît avec le temps sur la pente
    public float slopeSlideMaxSpeed = 30f;

    [Header("Air")]
    public float maxAirSpeed = 14f;
    public float accelAir = 50f;
    public float airControlAscendFactor = 0.6f;  // moins de contrôle en montée
    public float airControlDescendFactor = 1.0f; // plus en descente

    [Header("Gravity / Jump")]
    public float gravityBase = 25f;
    public float gravityMax = 60f;
    public float gravityRampTime = 0.8f;         // temps pour atteindre gravityMax en chute
    public float jumpHeight = 2.2f;
    public int maxJumps = 2;                   // 1 = simple, 2 = double, etc.
    public float coyoteTime = 0.12f;
    public float jumpBuffer = 0.15f;
    public float terminalVelocity = 75f;   // <-- AJOUT

    [Header("Dash (pour plus tard)")]
    public float dashStrength = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;

    [Header("Grounding")]
    public float groundCheckExtra = 0.08f; // marge sous la capsule
    public LayerMask groundMask = ~0;      // à filtrer selon projet
}
