using UnityEngine;
using System;

public class PlayerMovement
{
    public CharacterController characterController;
    public PlayerStats stats;
    public MovementState currentState;

    public Vector3 velocity;
    public float verticalVelocity;

    // Inputs
    public bool isSprinting;
    public bool jumpInput;
    public bool dashInput;
    public bool crouchInput;
    public bool attackInput = false;
    public bool crawlInput;

    public float originalHeight;

    // Observer pour notifier les changements d'état
    public event Action<string> OnStateChanged;

    public PlayerMovement(CharacterController controller, PlayerStats stats)
    {
        this.characterController = controller;
        this.stats = stats;
        originalHeight = controller.height;
        velocity = Vector3.zero;
        verticalVelocity = 0f;
        currentState = StatePool.GetWalkingState(this);
    }

    public void Move(Vector2 moveInput)
    {
        MovementState newState = currentState.CheckForStateChange(moveInput);
        if (newState != currentState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(currentState.GetType().Name);
        }
        currentState.HandleMovement(moveInput);
    }

    public void RestoreHitbox()
    {
        characterController.height = originalHeight;
        characterController.center = new Vector3(0, 0, 0);
    }
}