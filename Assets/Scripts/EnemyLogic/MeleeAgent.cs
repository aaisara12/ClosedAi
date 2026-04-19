using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyNavigator), typeof(PlayerDetector))]
public class MeleeAgent : EnemyAgent, IMovable
{
    [SerializeField] private float _attackCooldown = 2f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackDuration = 0.3f;
    [SerializeField] private Collider _attackCollider;

    public override EnemyType Type => EnemyType.Melee;
    public float CooldownMultiplier { get; set; } = 1f;

    private EnemyNavigator _nav;
    private Vector3 _targetPos;
    private bool _hasTarget;
    private float _nextAttackTime;
    private bool _isAttacking;

    protected override void Awake()
    {
        base.Awake();
        _nav = GetComponent<EnemyNavigator>();
        if (_attackCollider != null) _attackCollider.enabled = false;
    }

    private void Update()
    {
        if (!_hasTarget) return;

        float dist = Vector3.Distance(transform.position, _targetPos);
        if (dist > _attackRange)
        {
            _nav.MoveTo(_targetPos);
        }
        else
        {
            _nav.Stop();
            if (!_isAttacking && Time.time >= _nextAttackTime)
                StartCoroutine(AttackCoroutine());
        }
    }

    private IEnumerator AttackCoroutine()
    {
        _isAttacking = true;
        _nextAttackTime = Time.time + _attackCooldown * CooldownMultiplier;

        if (_attackCollider != null) _attackCollider.enabled = true;
        yield return new WaitForSeconds(_attackDuration);
        if (_attackCollider != null) _attackCollider.enabled = false;

        _isAttacking = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isAttacking) return;
        if (other.CompareTag("Player"))
            Debug.Log($"{name} hit the player");
    }

    public override void OnPlayerSpotted(Vector3 position)
    {
        _targetPos = position;
        _hasTarget = true;
    }

    // IMovable — strategies update the chase target via MoveTo each tick
    public void MoveTo(Vector3 position)
    {
        _targetPos = position;
        _hasTarget = true;
    }

    public void Stop()
    {
        _hasTarget = false;
        _nav.Stop();
    }

    public bool HasReached => _nav.HasReached;
}
