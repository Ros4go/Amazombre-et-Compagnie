using UnityEngine;
using System.Reflection;

public class PlayerDebug : MonoBehaviour
{
    [Header("Refs")]
    public Player player;

    [Header("UI")]
    public KeyCode toggleKey = KeyCode.F3;
    public bool visible = true;
    public Vector2 origin = new Vector2(12, 12);
    public float width = 420f;
    public int fontSize = 14;

    // barre de progression
    Texture2D _tex;
    GUIStyle _label;

    void Awake()
    {
        if (_tex == null)
        {
            _tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _tex.SetPixel(0, 0, Color.white);
            _tex.Apply();
        }
    }

    void OnGUI()
    {
        if (!visible || player == null) return;

        if (_label == null)
        {
            _label = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = true };
        }

        float line = 0f;
        Rect Area(float h) => new Rect(origin.x, origin.y + line, width, h);

        // Conteneur
        GUI.Box(Area(230f), GUIContent.none);
        float pad = 8f;
        Rect r = new Rect(origin.x + pad, origin.y + pad, width - pad * 2f, 999f);
        GUILayout.BeginArea(r);

        // Header
        GUILayout.Label("<b>Amazombre Debug</b>", _label);

        // State
        string state = (player.FSM != null && player.FSM.Current != null)
            ? player.FSM.Current.GetType().Name
            : "None";
        GUILayout.Label($"State: <b>{state}</b>", _label);

        // Vélocité
        Vector3 vel = player.velocity;
        Vector3 horiz = Vector3.ProjectOnPlane(vel, Vector3.up);
        GUILayout.Label($"Speed Horiz: <b>{horiz.magnitude:F2}</b>  |  Speed Tot: <b>{vel.magnitude:F2}</b>", _label);
        GUILayout.Label($"VelY: <b>{vel.y:F2}</b>   Grounded: <b>{player.isGrounded}</b>", _label);

        // Jumps
        int maxJumps = player.data != null ? player.data.maxJumps : 1;
        GUILayout.Label($"Jumps: <b>{player.jumpCount}</b> / <b>{maxJumps}</b>", _label);

        // Dashes
        int dashMax = player.data != null ? player.data.dashMaxCharges : 1;
        int dashNow = player.dashCharges;
        float cdRemain = GetPrivateFloat(player, "dashCooldownTimer");   // secondes avant début recharge
        float rechargeT = player.data != null ? Mathf.Max(0.0001f, player.data.dashRechargeTime) : 1f;
        float rechargeV = GetPrivateFloat(player, "dashRechargeTimer");  // temps accumulé sur recharge en cours
        float progress = Mathf.Clamp01(rechargeV / rechargeT);

        GUILayout.Label($"Dash: <b>{dashNow}</b> / <b>{dashMax}</b>", _label);

        if (cdRemain > 0f)
        {
            GUILayout.Label($"Cooldown: <b>{cdRemain:F2}s</b>", _label);
            DrawProgressBar("Recharge", 0f, Color.gray);
        }
        else if (dashNow < dashMax)
        {
            GUILayout.Label($"Recharge: <b>{progress * 100f:F0}%</b>", _label);
            DrawProgressBar("Recharge", progress, new Color(0.2f, 0.8f, 1f, 1f));
        }
        else
        {
            GUILayout.Label("Recharge: <b>OK</b>", _label);
            DrawProgressBar("Recharge", 1f, new Color(0.2f, 1f, 0.4f, 1f));
        }

        GUILayout.EndArea();
    }

    void DrawProgressBar(string label, float t, Color c)
    {
        float h = 18f;
        Rect bar = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(h));
        // fond
        GUI.color = new Color(0f, 0f, 0f, 0.35f);
        GUI.DrawTexture(bar, _tex);
        // fill
        GUI.color = c;
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(t), bar.height), _tex);
        // cadre
        GUI.color = Color.white;
        GUI.Box(bar, GUIContent.none);
        // label
        var centered = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = fontSize - 1 };
        GUI.Label(bar, label, centered);
        GUI.color = Color.white;
    }

    // Récupère un float privé par réflexion (debug only)
    static float GetPrivateFloat(object obj, string fieldName)
    {
        if (obj == null) return 0f;
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f != null && f.FieldType == typeof(float))
            return (float)f.GetValue(obj);
        return 0f;
    }
}
