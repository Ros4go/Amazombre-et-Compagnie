using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    [Header("Actions (Input System)")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;
    public InputActionReference dashAction;
    public InputActionReference slideAction;

    [Header("Runtime")]
    public Vector2 Move;      // -1..1
    public Vector2 Look;      // delta souris / stick
    public bool JumpPressed;  // press (buffer lu côté Player)
    public bool DashPressed;
    public bool SlideHeld;

    void OnEnable()
    {
        moveAction?.action.Enable();
        lookAction?.action.Enable();
        jumpAction?.action.Enable();
        dashAction?.action.Enable();
        slideAction?.action.Enable();

        jumpAction.action.performed += OnJump;
        dashAction.action.performed += OnDash;
    }

    void OnDisable()
    {
        jumpAction.action.performed -= OnJump;
        dashAction.action.performed -= OnDash;

        moveAction?.action.Disable();
        lookAction?.action.Disable();
        jumpAction?.action.Disable();
        dashAction?.action.Disable();
        slideAction?.action.Disable();
    }

    void Update()
    {
        Move = moveAction?.action.ReadValue<Vector2>() ?? Vector2.zero;
        Look = lookAction?.action.ReadValue<Vector2>() ?? Vector2.zero;
        SlideHeld = (slideAction != null) && slideAction.action.IsPressed();
        // JumpPressed/DashPressed sont des impulsions : consommées par Player.
    }

    void OnJump(InputAction.CallbackContext ctx) => JumpPressed = true;
    void OnDash(InputAction.CallbackContext ctx) => DashPressed = true;

    // Utilisées par le Player pour consommer les impulsions proprement
    public bool ConsumeJumpPressed() { var b = JumpPressed; JumpPressed = false; return b; }
    public bool ConsumeDashPressed() { var b = DashPressed; DashPressed = false; return b; }
}
