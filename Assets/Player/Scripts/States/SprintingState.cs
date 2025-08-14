using UnityEngine;

public class SprintingState : MovementState
{
    private Vector2 lastMoveInput;

    public SprintingState(PlayerMovement movement, Vector2 moveInput) : base(movement)
    {
        lastMoveInput = moveInput;
    }

    public void Reset(PlayerMovement movement, Vector2 moveInput)
    {
        this.movement = movement;
        lastMoveInput = moveInput;
    }

    public override void HandleMovement(Vector2 moveInput)
    {
        movement.RestoreHitbox();

        if (moveInput.sqrMagnitude > 1)
            moveInput = moveInput.normalized;

        Vector3 targetVelocity = new Vector3(moveInput.x, 0, moveInput.y) * movement.stats.sprintSpeed;
        targetVelocity = movement.characterController.transform.TransformDirection(targetVelocity);

        float accelerationFactor = (targetVelocity.x == 0 && targetVelocity.z == 0) ? movement.stats.deceleration : movement.stats.acceleration;
        movement.velocity.x = Mathf.Lerp(movement.velocity.x, targetVelocity.x, accelerationFactor * Time.deltaTime);
        movement.velocity.z = Mathf.Lerp(movement.velocity.z, targetVelocity.z, accelerationFactor * Time.deltaTime);

        if (movement.characterController.isGrounded)
            movement.verticalVelocity = -0.1f;
        else
            movement.verticalVelocity += movement.stats.gravity * Time.deltaTime;

        movement.velocity.y = movement.verticalVelocity;
        movement.characterController.Move(movement.velocity * Time.deltaTime);
    }

    public override MovementState CheckForStateChange(Vector2 moveInput)
    {
        if (movement.dashInput && movement.characterController.isGrounded)
            return StatePool.GetDashingState(movement, moveInput);

        if (movement.jumpInput && movement.characterController.isGrounded)
            return StatePool.GetJumpingState(movement);

        if (!movement.isSprinting)
            return StatePool.GetWalkingState(movement);

        if (movement.crouchInput)
        {
            if (movement.crawlInput)
                return StatePool.GetCrawlingState(movement, moveInput);
            return StatePool.GetCrouchingState(movement, moveInput);
        }

        return this;
    }
}
