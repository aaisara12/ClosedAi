using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyNavigator), typeof(PlayerDetector))]
public class RangedAgent : EnemyAgent, IMovable, IShooter
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _projectileSpeed = 15f;
    [SerializeField] private float _preFirePause = 0.4f;
    [SerializeField] private float _burstDuration = 1f;
    [SerializeField] private float _burstFireRate = 0.15f;
    [SerializeField] private float _attackCooldown = 3f;

    public override EnemyType Type => EnemyType.Ranged;
    public float CooldownMultiplier { get; set; } = 1f;

    private EnemyNavigator _nav;
    private bool _isFiring;
    private float _nextAttackTime;

    protected override void Awake()
    {
        base.Awake();
        _nav = GetComponent<EnemyNavigator>();
    }

    public void MoveTo(Vector3 position) => _nav.MoveTo(position);
    public void Stop() => _nav.Stop();
    public bool HasReached => _nav.HasReached;

    public void FacePosition(Vector3 worldPosition)
    {
        Vector3 dir = Vector3.ProjectOnPlane(worldPosition - transform.position, Vector3.up);
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    public void FireAt(Vector3 worldPosition)
    {
        if (_isFiring || Time.time < _nextAttackTime) return;
        StartCoroutine(FireCoroutine(worldPosition));
    }

    private IEnumerator FireCoroutine(Vector3 target)
    {
        _isFiring = true;
        _nextAttackTime = Time.time + _attackCooldown * CooldownMultiplier;

        yield return new WaitForSeconds(_preFirePause);

        float elapsed = 0f;
        while (elapsed < _burstDuration)
        {
            ShootProjectile(target);
            yield return new WaitForSeconds(_burstFireRate);
            elapsed += _burstFireRate;
        }

        _isFiring = false;
    }

    private void ShootProjectile(Vector3 target)
    {
        if (_projectilePrefab == null) return;
        Transform origin = _firePoint != null ? _firePoint : transform;
        Vector3 dir = (target - origin.position).normalized;
        GameObject proj = Instantiate(_projectilePrefab, origin.position, Quaternion.LookRotation(dir));
        if (proj.TryGetComponent(out Rigidbody rb))
            rb.linearVelocity = dir * _projectileSpeed;
    }
}
