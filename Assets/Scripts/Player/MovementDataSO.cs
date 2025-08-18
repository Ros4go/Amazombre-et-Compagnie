using UnityEngine;

[CreateAssetMenu(menuName = "Amazombre/Movement Data", fileName = "MovementData")]
public class MovementDataSO : ScriptableObject
{
    [Header("Gravity & Jump")]
    public float gravity = 30f;
    public float jumpHeight = 1.8f;
    public int maxJumps = 2;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Ground Move")]
    public float maxGroundSpeed = 10f;
    public float groundAccel = 60f;
    public float groundDecel = 50f;

    [Header("Air Move")]
    public float maxAirSpeed = 10f;
    public float airAccel = 25f;
    public float airDecel = 2f;

    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.18f;
    public int dashMaxCharges = 2;
    public float dashRechargeDelay = 0.8f;
    public float dashRechargeTime = 1.4f;

    [Header("Slide / Crouch")]
    public float crouchHeight = 1.1f;
    public float slideSpeed = 13f;
    public float slideCooldown = 0.25f;
    public float standClearTime = 0.08f;

    [Tooltip("Seuil d'input lors de l'appui initial pour prendre la direction de l'input, sinon la direction du regard.")]
    [Range(0f, 1f)] public float slideInputThreshold = 0.2f;

    [Tooltip("Taux de rotation max pendant le slide (deg/s). Plus bas = plus 'rail'. Ex: 25-45.")]
    public float slideTurnRateDeg = 32f;

    public float slideStickDownVel = 8f;
    public float slideSnapProbe = 0.6f;

    [Header("Crouch Walk")]
    public float crouchWalkSpeed = 5.5f;
    public float crouchAccel = 40f;
    public float crouchDecel = 35f;

    [Header("Slam")]
    public float slamDownForce = 40f;
    public float slamLockTime = 0.1f;

    [Header("Camera / Look")]
    [Range(0.05f, 20f)] public float mouseSensitivity = 6f;
    public float headbobAmplitude = 0.025f;
    public float headbobFrequency = 11f;
    public float camPitchClamp = 85f;

    [Header("Camera Crouch/Slide")]
    [Tooltip("Abaissement de la caméra lorsque crouch/slide (m).")]
    public float crouchCamOffset = 0.45f;
    [Tooltip("Vitesse de lissage de l'offset (1/s).")]
    public float camCrouchLerp = 12f;
}
