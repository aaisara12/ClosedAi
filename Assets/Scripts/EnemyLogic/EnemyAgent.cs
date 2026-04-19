using UnityEngine;

[RequireComponent(typeof(Health))]
public abstract class EnemyAgent : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float _patrolRadius = 8f;
    [SerializeField] private float _patrolPauseDuration = 1.5f;
    [SerializeField] private float _patrolScanDuration = 2f;
    [SerializeField] private float _patrolScanSpeed = 60f;

    public abstract EnemyType Type { get; }
    public GroupBrain Brain { get; set; }
    public Vector3 PatrolOrigin { get; private set; }
    public float PatrolRadius => _patrolRadius;
    public float PatrolPauseDuration => _patrolPauseDuration;
    public float PatrolScanDuration => _patrolScanDuration;
    public float PatrolScanSpeed => _patrolScanSpeed;

    protected virtual void Awake()
    {
        PatrolOrigin = transform.position;
        GetComponent<Health>()?.OnDeath.AddListener(Die);
    }

    // public void TakeDamage(float amount)
    // {
    //     _health -= amount;
    //     if (_health <= 0f) Die();
    // }

    private void Die()
    {
        Brain?.RemoveMember(this);
        Destroy(gameObject);
    }

    public virtual void OnPlayerSpotted(Vector3 position) { }
    public virtual void OnConnectionMade(EnemyAgent other) { }
    public virtual void OnConnectionLost(EnemyAgent other) { }
}
