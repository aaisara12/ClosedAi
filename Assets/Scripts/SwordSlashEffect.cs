using UnityEngine;

/// <summary>
/// Spawns a procedural arc mesh with the SwordSlash shader each time Spawn() is called.
/// Attach this component to the Player GameObject and assign the SwordSlash material.
/// </summary>
public class SwordSlashEffect : MonoBehaviour
{
    // ── Material ─────────────────────────────────────────────────────────────
    [Header("Material")]
    [Tooltip("Material using the Custom/SwordSlash shader.")]
    [SerializeField] private Material _baseMaterial;

    // ── Arc Geometry ─────────────────────────────────────────────────────────
    [Header("Arc Geometry")]
    [Tooltip("Total angular span of the arc in degrees.")]
    [SerializeField] private float _arcAngleDeg   = 120f;
    [Tooltip("Inner radius of the arc ribbon.")]
    [SerializeField] private float _arcInnerRadius = 0.28f;
    [Tooltip("Outer radius of the arc ribbon.")]
    [SerializeField] private float _arcOuterRadius = 0.55f;
    [Tooltip("Number of quad segments along the arc. Higher = smoother.")]
    [SerializeField] private int   _arcSegments    = 24;
    [Tooltip("Rotation of the arc plane around the forward axis (Z) in degrees — sets the slash orientation.")]
    [SerializeField] private float _slashAngleDeg  = -45f;

    // ── Timing ───────────────────────────────────────────────────────────────
    [Header("Timing")]
    [SerializeField] private float _duration = 0.35f;

    // ── Placement ────────────────────────────────────────────────────────────
    [Header("Placement")]
    [Tooltip("Distance in front of the camera where the arc is spawned.")]
    [SerializeField] private float _spawnDistance = 0.6f;

    // ── Colour & Intensity ───────────────────────────────────────────────────
    [Header("Colour & Intensity")]
    [ColorUsage(true, true)] [SerializeField] private Color _coreColor  = new Color(1.0f, 1.0f, 1.0f,  1f);
    [ColorUsage(true, true)] [SerializeField] private Color _midColor   = new Color(0.6f, 0.85f, 1.0f, 1f);
    [ColorUsage(true, true)] [SerializeField] private Color _outerColor = new Color(0.1f, 0.3f,  0.9f, 1f);
    [SerializeField] private float _emissiveBoost = 4f;
    [SerializeField] private float _glowWidth     = 0.25f;
    [SerializeField] private float _glowFalloff   = 2.5f;
    [SerializeField] private float _sweepSharpness = 6f;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this to spawn a slash effect in front of <paramref name="cam"/>.
    /// Each call creates an independent instance that manages its own lifetime.
    /// </summary>
    /// <param name="cam">The camera transform used for placement and orientation.</param>
    /// <param name="rollAngleDeg">Rotation around the camera's forward (Z) axis, applied after yaw.</param>
    /// <param name="yawAngleDeg">Rotation around the camera's up (Y) axis, applied before roll.</param>
    public void Spawn(Transform cam, float rollAngleDeg = 0f, float yawAngleDeg = -80f)
    {
        if (_baseMaterial == null)
        {
            Debug.LogWarning("[SwordSlashEffect] No base material assigned.");
            return;
        }

        // Build arc mesh
        Mesh mesh = GenerateArcMesh();

        // Create GameObject
        GameObject go = new GameObject("SwordSlash_Instance");
        go.hideFlags = HideFlags.DontSave;

        MeshFilter   mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mf.mesh = mesh;

        // Clone the material so each instance has its own _Age
        Material mat = new Material(_baseMaterial);
        mat.SetColor("_CoreColor",    _coreColor);
        mat.SetColor("_MidColor",     _midColor);
        mat.SetColor("_OuterColor",   _outerColor);
        mat.SetFloat("_EmissiveBoost", _emissiveBoost);
        mat.SetFloat("_GlowWidth",    _glowWidth);
        mat.SetFloat("_GlowFalloff",  _glowFalloff);
        mat.SetFloat("_SweepSharpness", _sweepSharpness);
        mat.SetFloat("_SlashAngle",   _slashAngleDeg);
        mat.SetFloat("_Duration",     _duration);
        mat.SetFloat("_Age",          0f);
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;

        // Position: fixed in world space at spawn time
        Vector3 spawnPos = cam.position + cam.forward * _spawnDistance;
        go.transform.position = spawnPos;

        // Orient: face the camera, yaw around the up axis, then roll around the forward axis
        go.transform.rotation = cam.rotation * Quaternion.AngleAxis(rollAngleDeg, Vector3.forward) *
                                Quaternion.AngleAxis(yawAngleDeg, Vector3.up);

        // Hand off lifetime management to a transient runner
        SlashRunner runner = go.AddComponent<SlashRunner>();
        runner.Init(mat, _duration, mesh);
    }

    // ── Arc Mesh Generation ───────────────────────────────────────────────────

    private Mesh GenerateArcMesh()
    {
        int   segs       = Mathf.Max(2, _arcSegments);
        float halfArc    = _arcAngleDeg * 0.5f * Mathf.Deg2Rad;
        float startAngle = -halfArc;
        float stepAngle  = _arcAngleDeg * Mathf.Deg2Rad / (segs - 1);

        int vertCount = segs * 2;
        Vector3[] verts  = new Vector3[vertCount];
        Vector2[] uvs    = new Vector2[vertCount];
        int[]     tris   = new int[(segs - 1) * 6];

        for (int i = 0; i < segs; i++)
        {
            float angle = startAngle + stepAngle * i;
            float u     = (float)i / (segs - 1);   // 0→1 along arc sweep direction

            // Inner vertex (V = 0)
            verts[i * 2]     = new Vector3(Mathf.Cos(angle) * _arcInnerRadius, Mathf.Sin(angle) * _arcInnerRadius, 0f);
            uvs  [i * 2]     = new Vector2(u, 0f);

            // Outer vertex (V = 1)
            verts[i * 2 + 1] = new Vector3(Mathf.Cos(angle) * _arcOuterRadius, Mathf.Sin(angle) * _arcOuterRadius, 0f);
            uvs  [i * 2 + 1] = new Vector2(u, 1f);
        }

        // Build quads (two triangles per segment gap)
        for (int i = 0; i < segs - 1; i++)
        {
            int t = i * 6;
            int v = i * 2;
            tris[t + 0] = v;
            tris[t + 1] = v + 1;
            tris[t + 2] = v + 2;
            tris[t + 3] = v + 2;
            tris[t + 4] = v + 1;
            tris[t + 5] = v + 3;
        }

        Mesh mesh = new Mesh();
        mesh.name = "SwordSlashArc";
        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Inner runner MonoBehaviour ────────────────────────────────────────────

    /// <summary>
    /// Ticks _Age on the cloned material and destroys the GameObject when finished.
    /// Lives on the same GameObject as the MeshRenderer.
    /// </summary>
    private class SlashRunner : MonoBehaviour
    {
        private Material _mat;
        private float    _duration;
        private float    _age;
        private Mesh     _mesh;

        public void Init(Material mat, float duration, Mesh mesh)
        {
            _mat      = mat;
            _duration = duration;
            _mesh     = mesh;
            _age      = 0f;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            _mat.SetFloat("_Age", _age);

            if (_age >= _duration)
            {
                Destroy(_mat);
                Destroy(_mesh);
                Destroy(gameObject);
            }
        }
    }
}

