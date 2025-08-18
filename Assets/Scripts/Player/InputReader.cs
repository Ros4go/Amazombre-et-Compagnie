using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputReader : MonoBehaviour
{
    [Header("Action Refs (drag depuis l'asset)")]
    public InputActionReference move;
    public InputActionReference look;
    public InputActionReference jump;
    public InputActionReference dash;
    public InputActionReference slide;

    [Header("Runtime")]
    public Vector2 Move;
    public Vector2 Look;
    public bool JumpPressed, JumpHeld;
    public bool DashPressed, SlidePressed, SlideHeld;

    bool _subscribed;

    void OnEnable()
    {
        TryEnable(move); if (move && move.action != null) { move.action.performed += OnMovePerf; move.action.canceled += OnMovePerf; _subscribed = true; }
        TryEnable(look); if (look && look.action != null) { look.action.performed += OnLookPerf; look.action.canceled += OnLookPerf; _subscribed = true; }

        TryEnable(jump); if (jump && jump.action != null) { jump.action.started += OnJumpStarted; jump.action.canceled += OnJumpCanceled; _subscribed = true; }
        TryEnable(dash); if (dash && dash.action != null) { dash.action.started += OnDashStarted; _subscribed = true; }
        TryEnable(slide); if (slide && slide.action != null) { slide.action.started += OnSlideStarted; slide.action.canceled += OnSlideCanceled; _subscribed = true; }
    }

    void OnDisable()
    {
        if (_subscribed)
        {
            if (move && move.action != null) { move.action.performed -= OnMovePerf; move.action.canceled -= OnMovePerf; }
            if (look && look.action != null) { look.action.performed -= OnLookPerf; look.action.canceled -= OnLookPerf; }
            if (jump && jump.action != null) { jump.action.started -= OnJumpStarted; jump.action.canceled -= OnJumpCanceled; }
            if (dash && dash.action != null) { dash.action.started -= OnDashStarted; }
            if (slide && slide.action != null) { slide.action.started -= OnSlideStarted; slide.action.canceled -= OnSlideCanceled; }
        }

        TryDisable(move); TryDisable(look); TryDisable(jump); TryDisable(dash); TryDisable(slide);
        _subscribed = false;
    }

    void OnMovePerf(InputAction.CallbackContext ctx) => Move = ctx.ReadValue<Vector2>();
    void OnLookPerf(InputAction.CallbackContext ctx) => Look = ctx.ReadValue<Vector2>();
    void OnJumpStarted(InputAction.CallbackContext ctx) { JumpPressed = true; JumpHeld = true; }
    void OnJumpCanceled(InputAction.CallbackContext ctx) { JumpHeld = false; }
    void OnDashStarted(InputAction.CallbackContext ctx) { DashPressed = true; }
    void OnSlideStarted(InputAction.CallbackContext ctx) { SlidePressed = true; SlideHeld = true; }
    void OnSlideCanceled(InputAction.CallbackContext ctx) { SlideHeld = false; }

    void LateUpdate()
    {
        // reset des "pressed" frame-based
        JumpPressed = DashPressed = SlidePressed = false;
    }

    static void TryEnable(InputActionReference aref)
    {
        if (aref != null && aref.action != null && !aref.action.enabled) aref.action.Enable();
    }
    static void TryDisable(InputActionReference aref)
    {
        if (aref != null && aref.action != null && aref.action.enabled) aref.action.Disable();
    }
}
