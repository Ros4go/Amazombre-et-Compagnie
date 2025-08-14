// Uses Unity Input System (com.unity.inputsystem).
// Assign your InputActionReferences in the inspector (from your .inputactions asset).
// Actions expected: Move (Vector2), Look (Vector2), Jump (Button), Dash (Button), Slide (Button), Sprint (Button - optional)
using UnityEngine;
using UnityEngine.InputSystem;

namespace Amazombre.Player.Core
{
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Input Actions (Input System)")]
        public InputActionReference Move;
        public InputActionReference Look;
        public InputActionReference Jump;
        public InputActionReference Dash;
        public InputActionReference Slide;
        public InputActionReference Sprint;

        private PlayerNetworkShim net;

        public Vector2 MoveAxis { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool DashPressed { get; private set; }
        public bool SlideHeld { get; private set; }
        public bool SprintHeld { get; private set; }

        void Awake()
        {
            net = GetComponent<PlayerNetworkShim>();
        }

        void OnEnable()
        {
            // Enable safely even if null (allows using just keyboard for a quick test)
            Move?.action?.Enable();
            Look?.action?.Enable();
            Jump?.action?.Enable();
            Dash?.action?.Enable();
            Slide?.action?.Enable();
            Sprint?.action?.Enable();
        }

        void OnDisable()
        {
            Move?.action?.Disable();
            Look?.action?.Disable();
            Jump?.action?.Disable();
            Dash?.action?.Disable();
            Slide?.action?.Disable();
            Sprint?.action?.Disable();
        }

        void Update()
        {
            if (net && !net.HasInputAuthority) { Clear(); return; }

            MoveAxis = Move ? Move.action.ReadValue<Vector2>() : Vector2.zero;
            LookDelta = Look ? Look.action.ReadValue<Vector2>() : Vector2.zero;

            // Edge-triggered buttons
            JumpPressed = Jump && Jump.action.WasPressedThisFrame();
            DashPressed = Dash && Dash.action.WasPressedThisFrame();
            SlideHeld = Slide && Slide.action.IsPressed();
            SprintHeld = Sprint && Sprint.action.IsPressed();
        }

        void Clear()
        {
            MoveAxis = Vector2.zero;
            LookDelta = Vector2.zero;
            JumpPressed = false;
            DashPressed = false;
            SlideHeld = false;
            SprintHeld = false;
        }
    }
}
