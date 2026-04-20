using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(CapsuleCollider))]
public class Signal : MonoBehaviour
{
    [Header("References")]
    public SignalManager ManagerA { get; private set; }
    public SignalManager ManagerB { get; private set; }

    [Header("Visual Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color lineColor = new Color(0f, 1f, 1f, 0.4f);

    [Header("Collision")]
    [SerializeField] private float hitboxRadius = 0.1f;
    // Assign a dedicated physics layer in the Inspector (e.g. "Signal")
    [SerializeField] private string signalLayer = "Signals";

    private LineRenderer _lineRenderer;
    private CapsuleCollider _capsule;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Called by SignalManager immediately after instantiation.</summary>
    public void Initialise(SignalManager a, SignalManager b)
    {
        ManagerA = a;
        ManagerB = b;

        SetupLineRenderer();
        SetupCollider();

        gameObject.layer = LayerMask.NameToLayer(signalLayer);
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Update()
    {
        if (ManagerA == null || ManagerB == null) return;

        Vector3 posA = ManagerA.transform.position;
        Vector3 posB = ManagerB.transform.position;
        Vector3 aToB = Vector3.Normalize(posB - posA) * 0.3f;

        // Update visual
        _lineRenderer.SetPosition(0, posA + aToB);
        _lineRenderer.SetPosition(1, posB - aToB);

        // Update hitbox to follow the line
        UpdateCapsule(posA, posB);
    }

    // ── Setup Helpers ─────────────────────────────────────────────────────────

    private void SetupLineRenderer()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.material = lineMaterial != null
            ? lineMaterial
            : new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
        _lineRenderer.useWorldSpace = true;
    }

    private void SetupCollider()
    {
        _capsule = GetComponent<CapsuleCollider>();
        _capsule.isTrigger = true;           // player detects via trigger, not physics push
        _capsule.radius = hitboxRadius;
        _capsule.direction = 2;              // Z-axis — we rotate the object to align it
    }

    private void UpdateCapsule(Vector3 posA, Vector3 posB)
    {
        Vector3 midpoint = (posA + posB) * 0.5f;
        float length = Vector3.Distance(posA, posB);

        transform.position = midpoint;
        transform.rotation = Quaternion.FromToRotation(Vector3.forward, posB - posA);

        _capsule.height = length;
        _capsule.center = Vector3.zero;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the other manager in this connection, or null if the given
    /// manager is not part of this signal.
    /// </summary>
    public SignalManager GetOther(SignalManager requester)
    {
        if (requester == ManagerA) return ManagerB;
        if (requester == ManagerB) return ManagerA;
        return null;
    }

    /// <summary>
    /// Destroys this signal and notifies both managers.
    /// Safe to call from player interaction scripts.
    /// </summary>
    public void Break()
    {
        ManagerA?.OnSignalBroken(this);
        ManagerB?.OnSignalBroken(this);
        Destroy(gameObject);
    }
}