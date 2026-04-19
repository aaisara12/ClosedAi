using UnityEngine;

public abstract class EnemyAgent : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;

    public abstract EnemyType Type { get; }
    public GroupBrain Brain { get; set; }

    private float _health;

    protected virtual void Awake()
    {
        _health = _maxHealth;
    }

    public void TakeDamage(float amount)
    {
        _health -= amount;
        if (_health <= 0f) Die();
    }

    private void Die()
    {
        Brain?.RemoveMember(this);
        Destroy(gameObject);
    }

    public virtual void OnPlayerSpotted(Vector3 position) { }
    public virtual void OnConnectionMade(EnemyAgent other) { }
    public virtual void OnConnectionLost(EnemyAgent other) { }
}
