using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;
    private PlayerMovement movement;

    private Vector2 moveInput;
    public Vector2 MoveInput { get { return moveInput; } }

    [SerializeField] private PlayerStats playerStats;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        characterController.slopeLimit = playerStats.maxSlopeLimit;
        characterController.stepOffset = playerStats.stepOffset;

        movement = new PlayerMovement(characterController, playerStats);
        movement.OnStateChanged += (newState) =>
        {
            //Debug.Log("Changement d'état vers : " + newState);
        };
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        movement.isSprinting = value.isPressed;
    }

    public void OnJump(InputValue value)
    {
        movement.jumpInput = value.isPressed;
    }

    public void OnDash(InputValue value)
    {
        movement.dashInput = value.isPressed;
    }

    public void OnCrouch(InputValue value)
    {
        movement.crouchInput = value.isPressed;
    }

    public void OnCrawl(InputValue value)
    {
        movement.crawlInput = value.isPressed;
    }

    public void OnAttack(InputValue value)
    {
        movement.attackInput = value.isPressed;
    }

    private void Update()
    {
        movement.Move(moveInput);

        // Anti-Spam Dash 
        if (movement.dashInput) movement.dashInput = false;
    }
}
