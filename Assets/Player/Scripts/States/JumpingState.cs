using UnityEngine;

public class JumpingState : MovementState
{
    private bool jumpInitiated;

    public JumpingState(PlayerMovement movement) : base(movement)
    {
        movement.verticalVelocity = movement.stats.jumpForce;
        jumpInitiated = true;
    }

    public void Reset(PlayerMovement movement)
    {
        this.movement = movement;
        movement.verticalVelocity = movement.stats.jumpForce;
        jumpInitiated = true;
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

        if (jumpInitiated)
            jumpInitiated = false;
        else if (movement.characterController.isGrounded)
            movement.verticalVelocity = -0.1f;
        else
            movement.verticalVelocity += movement.stats.gravity * movement.stats.gravityFactor * Time.deltaTime;

        movement.velocity.y = movement.verticalVelocity;
        movement.characterController.Move(movement.velocity * Time.deltaTime);
    }

    public override MovementState CheckForStateChange(Vector2 moveInput)
    {
        if (movement.characterController.isGrounded && !jumpInitiated)
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
