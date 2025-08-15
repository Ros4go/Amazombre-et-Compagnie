using UnityEngine;

public class PlayerDebug : MonoBehaviour
{
    [System.NonSerialized] public Player player;
    bool dirty;

    public void SetDirty() => dirty = true;

    void OnGUI()
    {
        if (player == null) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        GUILayout.BeginArea(new Rect(10, 10, 380, 240), GUI.skin.box);
        GUILayout.Label("<b>Amazombre Debug</b>", new GUIStyle(style) { richText = true });

        GUILayout.Label($"State: {(player.FSM.Current != null ? player.FSM.Current.GetType().Name : "None")}", style);
        var horiz = Vector3.ProjectOnPlane(player.velocity, Vector3.up);
        GUILayout.Label($"Speed (Horiz): {horiz.magnitude:F2}", style);
        GUILayout.Label($"Vel: {player.velocity}", style);
        GUILayout.Label($"Vel: {player.velocity}", style);
        GUILayout.Label($"Grounded: {player.isGrounded}", style);
        GUILayout.Label($"GroundNormal: {player.groundNormal}", style);
        GUILayout.Label($"Jumps: {player.jumpCount}/{(player.data != null ? player.data.maxJumps : 0)}", style);
        GUILayout.Label($"CoyoteTimer: {player.coyoteTimer:F3} / {(player.data != null ? player.data.coyoteTime : 0f):F3}", style);
        GUILayout.Label($"TimeInAir: {player.timeInAir:F3}", style);
        GUILayout.Label($"SlopeTimer: {player.slopeTimer:F3}", style);

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