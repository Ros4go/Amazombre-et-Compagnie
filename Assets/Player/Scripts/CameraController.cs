using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Cible & Références")]
    public Transform target;                   // Transform du joueur.
    public PlayerController playerController;  // Référence au PlayerController pour accéder aux inputs et modifier la rotation du joueur.

    [Header("Distances & Zoom")]
    public float distance = 5f;                // Distance actuelle.
    public float minDistance = 1f;             // Distance minimale (pour passer en vue FPS).
    public float maxDistance = 10f;            // Distance maximale.
    public float zoomSpeed = 2f;               // Vitesse de zoom.

    [Header("Camera")]
    public float playerRotationSpeed = 10f;    // Vitesse de rotation du player.

    [Header("Collision")]
    public float collisionRadius = 0.2f;             // Rayon pour le SphereCast de collision
    public LayerMask collisionLayers;                // Layers considérés pour la collision
    public float cameraCollisionOffset = 0.1f;         // Décalage pour éviter le clipping

    [Header("FPS HeadBob")]
    public float headBobFrequency = 1.5f;              // Fréquence du headbob
    public float headBobAmplitude = 0.05f;             // Amplitude du headbob

    [Header("Rotation & Angles TPS")]
    public float rotationSpeed = 5f;           // Vitesse de rotation de la caméra.
    public float verticalAngleMin = -20f;      // Angle vertical minimal en TPS.
    public float verticalAngleMax = 80f;       // Angle vertical maximal en TPS.

    [Header("Rotation & Angles FPS")]
    public float fpsVerticalAngleMin = -80f;   // Angle vertical minimal en FPS.
    public float fpsVerticalAngleMax = 80f;    // Angle vertical maximal en FPS.

    [Header("Offsets")]
    public Vector3 headOffset = new Vector3(0, 1.6f, 0); // Décalage pour positionner la caméra (position de la tête).

    // Variables internes
    private float headBobTimer = 0f;
    private float currentYaw = 0f;
    private float currentPitch = 10f;

    // Mode FPS si la distance est très proche
    private bool isFPS { get { return distance <= minDistance + 0.1f; } }

    void Start()
    {
        // Verrouille et cache le curseur pour un contrôle continu
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Application.runInBackground = true;
    }

    void Update()
    {
        HandleZoom();
        HandleRotation();
        UpdatePlayerFacing();
    }

    void LateUpdate()
    {
        UpdateCameraPosition();
        ApplyHeadBob();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        currentYaw += mouseX * rotationSpeed;
        currentPitch -= mouseY * rotationSpeed;
        if (isFPS)
        {
            currentPitch = Mathf.Clamp(currentPitch, fpsVerticalAngleMin, fpsVerticalAngleMax);
        }
        else
        {
            currentPitch = Mathf.Clamp(currentPitch, verticalAngleMin, verticalAngleMax);
        }
    }

    void UpdateCameraPosition()
    {
        if (isFPS)
        {
            // Mode FPS : la caméra se positionne à la tête du joueur.
            transform.position = target.position + headOffset;
            transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        }
        else
        {
            // Calcul de la position souhaitée en TPS
            Vector3 offset = new Vector3(0, 0, -distance);
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            Vector3 desiredPosition = target.position + rotation * offset;

            // Début du SphereCast à partir de la position de la tête
            Vector3 headPosition = target.position + headOffset;
            Vector3 direction = desiredPosition - headPosition;
            float desiredDistance = direction.magnitude;
            direction.Normalize();

            RaycastHit hit;
            if (Physics.SphereCast(headPosition, collisionRadius, direction, out hit, desiredDistance, collisionLayers))
            {
                float adjustedDistance = hit.distance - cameraCollisionOffset;
                adjustedDistance = Mathf.Clamp(adjustedDistance, minDistance, distance);
                transform.position = headPosition + direction * adjustedDistance;
            }
            else
            {
                transform.position = desiredPosition;
            }
            transform.LookAt(target.position + headOffset);
        }
    }

    void UpdatePlayerFacing()
    {
        if (playerController == null) return;
        Vector2 input = playerController.MoveInput;  // Le PlayerController doit exposer MoveInput.
        bool shouldUpdate = false;

        // En FPS, on met toujours à jour, en TPS dès qu'il y a un input (peu importe l'orientation)
        if (isFPS || input.magnitude > 0.1f)
            shouldUpdate = true;

        if (shouldUpdate)
        {
            PlayerFacingCamera();
        }
    }

    public void PlayerFacingCamera()
    {
        Vector3 camForward = transform.forward;
        camForward.y = 0; // On garde uniquement la direction horizontale.
        if (camForward.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camForward);
            playerController.transform.rotation = Quaternion.Slerp(playerController.transform.rotation, targetRotation, Time.deltaTime * playerRotationSpeed);
        }
    }

    public IEnumerator AlignPlayerToCamera()
    {
        float angleThreshold = 1f;

        if (playerController == null)
            yield break;

        while (true)
        {
            Vector3 camForward = transform.forward;
            camForward.y = 0;
            if (camForward.sqrMagnitude < 0.01f)
                yield break;

            Quaternion targetRotation = Quaternion.LookRotation(camForward);

            playerController.transform.rotation = Quaternion.Slerp(playerController.transform.rotation, targetRotation, Time.deltaTime * playerRotationSpeed);

            if (Quaternion.Angle(playerController.transform.rotation, targetRotation) < angleThreshold)
            {
                playerController.transform.rotation = targetRotation;
                break;
            }

            yield return null; // Attendre la frame suivante
        }
    }

    void ApplyHeadBob()
    {
        // Appliquer le headbob uniquement en vue FPS et si le joueur se déplace
        if (isFPS && playerController != null && playerController.MoveInput.magnitude > 0.1f)
        {
            headBobTimer += Time.deltaTime * headBobFrequency;
            float bobOffset = Mathf.Sin(headBobTimer) * headBobAmplitude;
            // Appliquer l'offset vertical
            transform.position += new Vector3(0, bobOffset, 0);
        }
        else
        {
            headBobTimer = 0f;
        }
    }

    public Vector3 GetCameraDirection()
    {
        return transform.forward.normalized;
    }
}
