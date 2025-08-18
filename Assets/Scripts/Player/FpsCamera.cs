using UnityEngine;

public class FpsCamera : MonoBehaviour
{
    public Player player;      // assigne dans l’Inspector
    public Transform yawRoot;  // généralement Player.transform
    public Transform pitchRoot;// enfant "CameraPivot" qui porte la Camera

    float camYOffset;      // offset lissé (vers le bas quand crouch)
    float camYOffsetVel;   // vel pour SmoothDamp (si tu préfères SmoothDamp)

    float pitch;
    float bobT;
    bool ready;

    // Facteur interne pour lisser les différences de devices (mouse/gamepad)
    const float lookScale = 0.02f;

    void Awake()
    {
        ready = (player && yawRoot && pitchRoot);
    }

    void Start()
    {
        if (!ready) ready = (player && yawRoot && pitchRoot);
    }

    void Update()
    {
        if (!ready || !player || !player.data || !player.input) return;

        // Sensibilité: data.mouseSensitivity (réglable dans l'Inspector)
        Vector2 look = player.input.Look * (player.data.mouseSensitivity * lookScale);

        yawRoot.Rotate(0f, look.x, 0f, Space.Self);

        pitch = Mathf.Clamp(pitch - look.y, -player.data.camPitchClamp, player.data.camPitchClamp);
        var e = pitchRoot.localEulerAngles; e.x = pitch; e.y = 0f; e.z = 0f;
        pitchRoot.localEulerAngles = e;

        // --- Headbob calcul ---
        Vector3 vPlanar = new Vector3(player.velocity.x, 0f, player.velocity.z);
        float speedRatio = Mathf.Clamp01(vPlanar.magnitude / player.data.maxGroundSpeed);
        float bob = 0f;
        if (speedRatio > 0.01f && player.isGrounded)
        {
            bobT += Time.deltaTime * player.data.headbobFrequency * Mathf.Lerp(0.5f, 1f, speedRatio);
            bob = Mathf.Sin(bobT) * player.data.headbobAmplitude * speedRatio;
        }
        else
        {
            bobT = 0f;
        }

        // --- Offset crouch/slide lissé ---
        float targetBase = (player.isCrouched ? -player.data.crouchCamOffset : 0f);
        // Méthode A (expo lisse) :
        camYOffset = Mathf.Lerp(camYOffset, targetBase, 1f - Mathf.Exp(-player.data.camCrouchLerp * Time.deltaTime));

        // (ou Méthode B si tu préfères SmoothDamp) :
        // camYOffset = Mathf.SmoothDamp(camYOffset, targetBase, ref camYOffsetVel, 1f / Mathf.Max(0.0001f, player.data.camCrouchLerp));

        pitchRoot.localPosition = new Vector3(0f, camYOffset + bob, 0f);
    }

    public void Kick(float intensity = 0.6f)
    {
        if (!ready || !player || !player.data) return;
        pitch = Mathf.Clamp(pitch - intensity * 3f, -player.data.camPitchClamp, player.data.camPitchClamp);
    }
}
