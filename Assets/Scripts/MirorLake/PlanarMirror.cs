using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class PlanarReflectionSimple : MonoBehaviour
{
    [Header("Setup")]
    public RenderTexture reflectionRT;
    public LayerMask reflectionCulling = ~0;  // tout par défaut
    public string waterLayerName = "Water";

    [Header("Tuning")]
    public float waterHeight = 0f; // y du plan d’eau
    public Color clearColor = Color.black;
    public bool clearSkybox = true;

    Camera _reflectionCam;
    Renderer _rend;
    Material _mat;
    int _waterLayer = -1;

    static readonly int _ReflectionTexID = Shader.PropertyToID("_ReflectionTex");
    static readonly int _ProjMatrixID = Shader.PropertyToID("_ReflectionVP");

    void OnEnable()
    {
        _rend = GetComponent<Renderer>();
        _mat = _rend.sharedMaterial;

        if (!_mat)
            Debug.LogWarning("Pas de matériel sur l'eau.");

        if (!reflectionRT)
            Debug.LogWarning("Assigne une RenderTexture (reflectionRT).");

        _waterLayer = LayerMask.NameToLayer(waterLayerName);

        EnsureCamera();
        ApplyStaticSetup();
    }

    void OnDisable()
    {
        if (_reflectionCam)
        {
            if (Application.isEditor)
                DestroyImmediate(_reflectionCam.gameObject);
            else
                Destroy(_reflectionCam.gameObject);
        }
    }

    void EnsureCamera()
    {
        if (_reflectionCam) return;

        var go = new GameObject("ReflectionCamera");
        go.hideFlags = HideFlags.DontSave;
        _reflectionCam = go.AddComponent<Camera>();

        // Sécurité : pas utilisée par défaut
        _reflectionCam.enabled = false;
        _reflectionCam.tag = "Untagged";
        var al = _reflectionCam.GetComponent<AudioListener>();
        if (al) DestroyImmediate(al);

        _reflectionCam.depthTextureMode = DepthTextureMode.None;
    }

    void ApplyStaticSetup()
    {
        if (_reflectionCam)
        {
            _reflectionCam.targetTexture = reflectionRT;
            _reflectionCam.cullingMask = reflectionCulling;

            // Exclure la surface d’eau
            if (_waterLayer >= 0)
                _reflectionCam.cullingMask &= ~(1 << _waterLayer);

            _reflectionCam.clearFlags = clearSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            _reflectionCam.backgroundColor = clearColor;
        }
    }

    void LateUpdate()
    {
        if (!_reflectionCam || reflectionRT == null) return;
        var mainCam = Camera.main;
        if (!mainCam) return;

        // 0) Assure un RT à même aspect que la main camera (évite bords coupés)
        EnsureRTMatchesMain(mainCam);

        // Param optiques identiques
        _reflectionCam.fieldOfView = mainCam.fieldOfView;
        _reflectionCam.nearClipPlane = mainCam.nearClipPlane;
        _reflectionCam.farClipPlane = mainCam.farClipPlane;
        _reflectionCam.orthographic = false;

        // --- Plan (y = waterHeight) ---
        Vector3 n = Vector3.up;
        float d = -Vector3.Dot(n, new Vector3(0f, waterHeight, 0f)); // n·x + d = 0
        Vector4 planeWorld = new Vector4(n.x, n.y, n.z, d);

        // (A) TRANSFORM miroir (pour la skybox)
        Vector3 pos = mainCam.transform.position;
        float dist = Vector3.Dot(n, pos) + d;
        Vector3 mirroredPos = pos - 2f * dist * n;

        Vector3 fwd = Vector3.Reflect(mainCam.transform.forward, n);
        Vector3 up = Vector3.Reflect(mainCam.transform.up, n);
        Quaternion mirroredRot = Quaternion.LookRotation(fwd, up);

        _reflectionCam.transform.SetPositionAndRotation(mirroredPos, mirroredRot);

        // (B) VUE miroir exacte via matrice (pour la géométrie & culling)
        Matrix4x4 reflectMat = CalculateReflectionMatrix(planeWorld);
        Matrix4x4 worldToCameraMain = mainCam.worldToCameraMatrix;
        Matrix4x4 worldToCameraRefl = worldToCameraMain * reflectMat;
        _reflectionCam.worldToCameraMatrix = worldToCameraRefl;

        // Plan de clip oblique en espace caméra de réflexion
        Vector4 clipPlaneCamera = CameraSpacePlane(worldToCameraRefl, planeWorld);

        // Proj oblique (utilise le near/far/FOV de la reflectionCam)
        _reflectionCam.projectionMatrix = _reflectionCam.CalculateObliqueMatrix(clipPlaneCamera);

        // Skybox identique
        var sky = _reflectionCam.GetComponent<Skybox>();
        if (!sky) sky = _reflectionCam.gameObject.AddComponent<Skybox>();
        sky.material = RenderSettings.skybox;
        _reflectionCam.clearFlags = CameraClearFlags.Skybox;

        // Rendu (handedness inversé -> on inverse le culling pendant le Render)
        GL.invertCulling = true;
        _reflectionCam.Render();
        GL.invertCulling = false;

        // Envoi au mat
        if (_mat)
        {
            _mat.SetTexture(_ReflectionTexID, reflectionRT);
            Matrix4x4 vp = _reflectionCam.projectionMatrix * _reflectionCam.worldToCameraMatrix;
            _mat.SetMatrix(_ProjMatrixID, vp);
        }
    }

    // --- Aspect/RT helper ---
    [Header("RenderTexture")]
    public bool autoMatchRTAspect = true;
    [Range(256, 4096)] public int rtBaseHeight = 1024;
    public int rtMSAA = 1;

    void EnsureRTMatchesMain(Camera mainCam)
    {
        if (!autoMatchRTAspect || reflectionRT == null) return;

        int targetH = Mathf.Max(64, rtBaseHeight);
        int targetW = Mathf.Max(64, Mathf.RoundToInt(targetH * mainCam.aspect));

        if (reflectionRT.width != targetW || reflectionRT.height != targetH)
        {
            bool wasCreated = reflectionRT.IsCreated();
            reflectionRT.Release();
            reflectionRT.width = targetW;
            reflectionRT.height = targetH;
            reflectionRT.antiAliasing = Mathf.Max(1, rtMSAA);
            if (wasCreated) reflectionRT.Create();
        }

        _reflectionCam.targetTexture = reflectionRT;
    }

    // --- Utils ---

    static Vector3 ReflectPoint(Vector3 p, Vector4 planeWorld)
    {
        Vector3 n = new Vector3(planeWorld.x, planeWorld.y, planeWorld.z);
        float d = planeWorld.w;
        float dist = Vector3.Dot(n, p) + d;
        return p - 2f * dist * n;
    }

    static Matrix4x4 CalculateReflectionMatrix(Vector4 p)
    {
        // p : (nx, ny, nz, d) pour plan nx*x + ny*y + nz*z + d = 0
        float nx = p.x, ny = p.y, nz = p.z, d = p.w;
        Matrix4x4 m = Matrix4x4.identity;

        m.m00 = 1f - 2f * nx * nx;
        m.m01 = -2f * nx * ny;
        m.m02 = -2f * nx * nz;
        m.m03 = -2f * nx * d;

        m.m10 = -2f * ny * nx;
        m.m11 = 1f - 2f * ny * ny;
        m.m12 = -2f * ny * nz;
        m.m13 = -2f * ny * d;

        m.m20 = -2f * nz * nx;
        m.m21 = -2f * nz * ny;
        m.m22 = 1f - 2f * nz * nz;
        m.m23 = -2f * nz * d;

        m.m30 = 0f; m.m31 = 0f; m.m32 = 0f; m.m33 = 1f;
        return m;
    }

    static Vector4 CameraSpacePlane(Matrix4x4 worldToCamera, Vector4 planeWorld)
    {
        // Transforme le plan en espace caméra de la réflexion
        Matrix4x4 m = worldToCamera.inverse.transpose;
        Vector4 p = m * planeWorld;
        // Normalisation
        float mag = Mathf.Sqrt(p.x * p.x + p.y * p.y + p.z * p.z);
        return p / mag;
    }
}
