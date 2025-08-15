using UnityEngine;

public class PlayerDebug : MonoBehaviour
{
    [System.NonSerialized] public Player player;

    [Header("UI")]
    public KeyCode toggleKey = KeyCode.F3;
    public bool visible = true;
    public Vector2 origin = new Vector2(10, 10);
    public Vector2 size = new Vector2(460, 340);
    public int fontSize = 14;

    bool dirty;

    public void SetDirty() => dirty = true;

    void OnGUI()
    {
        if (!visible || player == null) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = true };
        GUILayout.BeginArea(new Rect(origin.x, origin.y, size.x, size.y), GUI.skin.box);

        // Header
        GUILayout.Label("<b>Amazombre Debug</b>", style);

        // State
        string stateName = player.FSM != null && player.FSM.Current != null
            ? player.FSM.Current.GetType().Name
            : "None";
        GUILayout.Label($"State: {stateName}", style);

        // Vitesse
        var horiz = Vector3.ProjectOnPlane(player.velocity, Vector3.up);
        GUILayout.Label($"Speed Horiz: {horiz.magnitude:F2}", style);
        GUILayout.Label($"Vel: {player.velocity}", style);
        GUILayout.Label($"VelY: {player.velocity.y:F2}", style);

        // Grounding / Pente
        GUILayout.Space(4);
        GUILayout.Label("<b>Ground</b>", style);
        GUILayout.Label($"Grounded: {player.isGrounded}", style);
        GUILayout.Label($"NearSteepSlope: {player.nearSteepSlope}", style);
        GUILayout.Label($"GroundDist: {player.groundDistance:F3}", style);
        GUILayout.Label($"GroundNormal: {player.groundNormal}", style);
        GUILayout.Label($"SlopeAngle: {player.SlopeAngleDeg:F1} deg", style);
        bool tooSteep = player.IsTooSteep(player.groundNormal);
        GUILayout.Label($"IsTooSteep: {tooSteep} (walkableLimit={player.Controller.slopeLimit:F1} deg / slideThreshold={player.data.slopeSlideThresholdDeg:F1} deg)", style);

        // Jumps / Timers
        GUILayout.Space(4);
        GUILayout.Label("<b>Jump</b>", style);
        GUILayout.Label($"Jumps: {player.jumpCount}/{player.data.maxJumps}", style);
        GUILayout.Label($"Coyote: {player.coyoteTimer:F3} / {player.data.coyoteTime:F3}", style);
        GUILayout.Label($"TimeInAir: {player.timeInAir:F3}", style);
        GUILayout.Label($"SlopeTimer: {player.slopeTimer:F3}", style);

        // Inputs
        GUILayout.Space(4);
        GUILayout.Label("<b>Input</b>", style);
        if (player.input != null)
        {
            GUILayout.Label($"Move: {player.input.Move}", style);
            GUILayout.Label($"Look: {player.input.Look}", style);
        }
        else
        {
            GUILayout.Label("Input: (null)", style);
        }

        // Params utiles
        GUILayout.Space(4);
        GUILayout.Label("<b>Params</b>", style);
        GUILayout.Label($"MaxGroundSpeed: {player.data.maxGroundSpeed:F1}  |  MaxAirSpeed: {player.data.maxAirSpeed:F1}", style);
        GUILayout.Label($"Uphill scales  speed={player.data.uphillSpeedScale:F2}  accel={player.data.uphillAccelScale:F2}", style);

        GUILayout.EndArea();
        dirty = false;
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.yellow;
        Vector3 p = player.transform.position + Vector3.up * 0.05f;
        Gizmos.DrawRay(p, -player.groundNormal * 1.0f);
    }
}
