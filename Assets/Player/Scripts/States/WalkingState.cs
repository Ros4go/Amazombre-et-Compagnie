using UnityEngine;

public class WalkingState : MovementState
{
    public WalkingState(PlayerMovement movement) : base(movement) { }

    public void Reset(PlayerMovement movement)
    {
        this.movement = movement;
    }

    public override void HandleMovement(Vector2 moveInput)
    {
        movement.RestoreHitbox();

        if (moveInput.sqrMagnitude > 1)
            moveInput = moveInput.normalized;

        Vector3 targetVelocity = new Vector3(moveInput.x, 0, moveInput.y) * movement.stats.maxSpeed;
        targetVelocity = movement.characterController.transform.TransformDirection(targetVelocity);

        float accelerationFactor = (targetVelocity.x == 0 && targetVelocity.z == 0) ? movement.stats.deceleration : movement.stats.acceleration;
        movement.velocity.x = Mathf.Lerp(movement.velocity.x, targetVelocity.x, accelerationFactor * Time.deltaTime);
        movement.velocity.z = Mathf.Lerp(movement.velocity.z, targetVelocity.z, accelerationFactor * Time.deltaTime);

        if (movement.characterController.isGrounded)
            movement.verticalVelocity = -0.1f;
        else
            movement.verticalVelocity += movement.stats.gravity * movement.stats.gravityFactor * Time.deltaTime;

        movement.velocity.y = movement.verticalVelocity;

        ApplySlopeSlide();

        movement.characterController.Move(movement.velocity * Time.deltaTime);
    }

    public override MovementState CheckForStateChange(Vector2 moveInput)
    {
        if (movement.dashInput && movement.characterController.isGrounded)
            return StatePool.GetDashingState(movement, moveInput);

        if (movement.jumpInput && movement.characterController.isGrounded)
            return StatePool.GetJumpingState(movement);

        if (movement.crouchInput)
        {
            if (movement.crawlInput)
                return StatePool.GetCrawlingState(movement, moveInput);
            return StatePool.GetCrouchingState(movement, moveInput);
        }

        if (movement.isSprinting)
            return StatePool.GetSprintingState(movement, moveInput);

        return this;
    }
}
