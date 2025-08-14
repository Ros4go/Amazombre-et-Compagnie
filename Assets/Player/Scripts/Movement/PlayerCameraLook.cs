using UnityEngine;

namespace Amazombre.Player.Movement
{
    // Minimal, input-agnostic camera pitch/yaw with clamp and sensitivity.
    public class PlayerCameraLook : MonoBehaviour
    {
        [Header("Refs")]
        public Transform cameraPivot;  // empty object rotating on X (pitch)
        public Transform bodyYaw;      // player root rotating on Y (yaw)
        public Core.PlayerInputReader input;

        [Header("Settings")]
        public float mouseSensitivity = 0.12f;
        public float controllerSensitivity = 4f;
        public float pitchMin = -80f;
        public float pitchMax = 80f;

        private float pitch;

        void Reset()
        {
            if (!cameraPivot && Camera.main) cameraPivot = Camera.main.transform;
            if (!bodyYaw) bodyYaw = transform;
        }

        void Update()
        {
            if (!input) return;
            var look = input.LookDelta;

            // Heuristic: scale based on delta magnitude (mouse vs controller)
            float sens = (look.sqrMagnitude > 9f) ? controllerSensitivity : mouseSensitivity;

            // Apply yaw to body
            bodyYaw.Rotate(0f, look.x * sens, 0f, Space.Self);

            // Apply pitch to pivot
            pitch -= look.y * sens;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            var e = cameraPivot.localEulerAngles;
            e.x = pitch;
            cameraPivot.localEulerAngles = e;
        }
    }
}
