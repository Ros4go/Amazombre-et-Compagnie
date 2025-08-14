using UnityEngine;

public abstract class MovementState
{
    public PlayerMovement movement;

    public MovementState(PlayerMovement movement)
    {
        this.movement = movement;
    }

    public abstract void HandleMovement(Vector2 moveInput);
    public abstract MovementState CheckForStateChange(Vector2 moveInput);

    protected void ApplySlopeSlide()
    {
        if (!movement.characterController.isGrounded) return;

        RaycastHit hit;
        Vector3 sphereOrigin = movement.characterController.transform.position + Vector3.up * movement.characterController.radius;
        float sphereRadius = movement.characterController.radius;

        float sphereDistance = (movement.characterController.height - movement.characterController.radius) + 0.5f;
        if (Physics.SphereCast(sphereOrigin, sphereRadius, Vector3.down, out hit, sphereDistance))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > movement.stats.slopeLimit)
            {
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                movement.velocity += slideDirection * movement.stats.slideSpeed * (slopeAngle / 10) * Time.deltaTime;
            }
        }
    }

}
