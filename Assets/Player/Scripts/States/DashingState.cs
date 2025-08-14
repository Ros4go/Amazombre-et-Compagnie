using UnityEngine;

public class DashingState : MovementState
{
    private float dashTimer;
    private Vector3 dashDirection;

    public DashingState(PlayerMovement movement, Vector2 moveInput) : base(movement)
    {
        dashTimer = movement.stats.dashDuration;
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y);
        if (inputDirection.sqrMagnitude > 0.01f)
            dashDirection = movement.characterController.transform.TransformDirection(inputDirection.normalized);
        else
            dashDirection = movement.characterController.transform.forward;
    }

    public void Reset(PlayerMovement movement, Vector2 moveInput)
    {
        this.movement = movement;
        dashTimer = movement.stats.dashDuration;
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y);
        if (inputDirection.sqrMagnitude > 0.01f)
            dashDirection = movement.characterController.transform.TransformDirection(inputDirection.normalized);
        else
            dashDirection = movement.characterController.transform.forward;
    }

    public override void HandleMovement(Vector2 moveInput)
    {
        Vector3 dashVelocity = dashDirection * movement.stats.dashSpeed;
        movement.velocity = dashVelocity;
        movement.verticalVelocity = 0f;
        movement.velocity.y = movement.verticalVelocity;
        movement.characterController.Move(movement.velocity * Time.deltaTime);

        dashTimer -= Time.deltaTime;
    }

    public override MovementState CheckForStateChange(Vector2 moveInput)
    {
        if (dashTimer <= 0f)
        {
            if (movement.crouchInput)
            {
                if (movement.crawlInput)
                    return StatePool.GetCrawlingState(movement, moveInput);
                return StatePool.GetCrouchingState(movement, moveInput);
            }
            return movement.isSprinting ? StatePool.GetSprintingState(movement, moveInput) : StatePool.GetWalkingState(movement);
        }
        return this;
    }
}
