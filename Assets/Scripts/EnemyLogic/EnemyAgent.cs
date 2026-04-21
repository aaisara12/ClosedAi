using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public abstract class EnemyAgent : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float _patrolRadius = 8f;
    [SerializeField] private float _patrolPauseDuration = 1.5f;
    [SerializeField] private float _patrolScanDuration = 2f;
    [SerializeField] private float _patrolScanSpeed = 60f;

    [Header("Isolation")]
    [SerializeField] private float _isolationDuration = 4f;

    public abstract EnemyType Type { get; }
    public GroupBrain Brain { get; set; }
    public bool IsIsolated { get; private set; }
    public Vector3 PatrolOrigin { get; private set; }
    public float PatrolRadius => _patrolRadius;
    public float PatrolPauseDuration => _patrolPauseDuration;
    public float PatrolScanDuration => _patrolScanDuration;
    public float PatrolScanSpeed => _patrolScanSpeed;

    private SignalManager _signalManager;

    protected virtual void Awake()
    {
        PatrolOrigin = transform.position;
        GetComponent<Health>()?.OnDeath.AddListener(Die);
        _signalManager = GetComponentInChildren<SignalManager>();
        if (_signalManager != null) _signalManager.OnFullyIsolated += HandleFullyIsolated;
    }

    protected virtual void OnDestroy()
    {
        if (_signalManager != null) _signalManager.OnFullyIsolated -= HandleFullyIsolated;
    }

    private void HandleFullyIsolated()
    {
        if (IsIsolated) return;
        StartCoroutine(IsolationCoroutine());
    }

    private IEnumerator IsolationCoroutine()
    {
        IsIsolated = true;
        if (this is IMovable movable) movable.Stop();

        var detector = GetComponent<PlayerDetector>();
        var model = GetComponentInChildren<MeshRenderer>();
        float elapsed = 0f;

        Quaternion startRot = model.transform.localRotation;
        Quaternion flippedRot = startRot * Quaternion.Euler(180f, 0f, 0f);

        if (detector != null) detector.enabled = false;

        if (model != null)
        {
            while (elapsed < _isolationDuration)
            {
                model.transform.localRotation = 
                    Quaternion.LerpUnclamped(startRot, flippedRot, Mathf.Sin((elapsed / _isolationDuration) * (4 * Mathf.PI)));
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(_isolationDuration);
        }

        IsIsolated = false;
        if (detector != null) detector.enabled = true;
    }

    private void Die()
    {
        Brain?.RemoveMember(this);
        AudioSystem.Play(AudioSystem.Sound.EnemyDeath);
        Destroy(gameObject);
    }

    public virtual void OnPlayerSpotted(Vector3 position) { }
    public virtual void OnConnectionMade(EnemyAgent other) { }
    public virtual void OnConnectionLost(EnemyAgent other) { }
}
