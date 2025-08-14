using UnityEngine;

public static class StatePool
{
    private static WalkingState walkingState;
    private static SprintingState sprintingState;
    private static JumpingState jumpingState;
    private static DashingState dashingState;
    private static CrouchingState crouchingState;
    private static CrawlingState crawlingState;

    public static WalkingState GetWalkingState(PlayerMovement movement)
    {
        if (walkingState == null)
            walkingState = new WalkingState(movement);
        else
            walkingState.Reset(movement);
        return walkingState;
    }

    public static SprintingState GetSprintingState(PlayerMovement movement, Vector2 moveInput)
    {
        if (sprintingState == null)
            sprintingState = new SprintingState(movement, moveInput);
        else
            sprintingState.Reset(movement, moveInput);
        return sprintingState;
    }

    public static JumpingState GetJumpingState(PlayerMovement movement)
    {
        if (jumpingState == null)
            jumpingState = new JumpingState(movement);
        else
            jumpingState.Reset(movement);
        return jumpingState;
    }

    public static DashingState GetDashingState(PlayerMovement movement, Vector2 moveInput)
    {
        if (dashingState == null)
            dashingState = new DashingState(movement, moveInput);
        else
            dashingState.Reset(movement, moveInput);
        return dashingState;
    }

    public static CrouchingState GetCrouchingState(PlayerMovement movement, Vector2 moveInput)
    {
        if (crouchingState == null)
            crouchingState = new CrouchingState(movement, moveInput);
        else
            crouchingState.Reset(movement, moveInput);
        return crouchingState;
    }

    public static CrawlingState GetCrawlingState(PlayerMovement movement, Vector2 moveInput)
    {
        if (crawlingState == null)
            crawlingState = new CrawlingState(movement, moveInput);
        else
            crawlingState.Reset(movement, moveInput);
        return crawlingState;
    }
}
