using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Vitesse")]
    public float maxSpeed = 9f;
    public float sprintSpeed = 12f;
    public float crouchSpeed = 3f;
    public float crawlSpeed = 2f;

    [Header("Accélération")]
    public float acceleration = 15f;
    public float deceleration = 30f;

    [Header("Physique")]
    public float gravity = -9.81f;
    public float gravityFactor = 5f;

    [Header("Pentes")]
    public float maxSlopeLimit = 55f;
    public float stepOffset = 0.3f;
    public float slopeLimit = 40f;
    public float slideSpeed = 35f;

    [Header("Saut")]
    public float jumpForce = 15f;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.3f;
}
