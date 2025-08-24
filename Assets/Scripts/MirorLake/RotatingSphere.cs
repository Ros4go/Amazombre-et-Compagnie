using UnityEngine;

public class DoubleRotation : MonoBehaviour
{
    [SerializeField] private float rotationSpeedX = 45f;
    [SerializeField] private float rotationSpeedY = 30f;
    [SerializeField] private float skyboxRotationSpeed = 1f;


    void Update()
    {
        // Tourne en continu autour de l'axe X et de l'axe Y
        transform.Rotate(Vector3.right * rotationSpeedX * Time.deltaTime, Space.Self);
        transform.Rotate(Vector3.up * rotationSpeedY * Time.deltaTime, Space.Self);
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * skyboxRotationSpeed);
    }
}
