Input Actions expected (Unity Input System):

Action Map: "Player"
- Move : Vector2 [WASD / Left Stick]
- Look : Vector2 [Mouse Delta / Right Stick]
- Jump : Button [Space / South]
- Dash : Button [Left Shift (tap) / East]
- Slide: Button [Left Ctrl (hold) / B]
- Sprint: Button [Left Shift (hold) / L3]

Drag your generated InputAction asset into PlayerInputReader fields.
Attach PlayerNetworkShim, PlayerInputReader, PlayerCameraLook, PlayerMotor, PlayerStateMachine to your Player prefab.
Assign CharacterController and a Camera pivot (an empty under the camera) to PlayerCameraLook.
Set LayerMask "wallMask" to the walls you can wallrun.
