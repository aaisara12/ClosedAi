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
    [SerializeField] private float _turnSpeed = 120f;

    public override EnemyType Type => EnemyType.Ranged;
    public float CooldownMultiplier { get; set; } = 1f;

    private EnemyNavigator _nav;
    private bool _isFiring;
    private float _nextAttackTime;
    private Vector3 _currentTarget;

    protected override void Awake()
    {
        base.Awake();
        _nav = GetComponent<EnemyNavigator>();
    }

    public void MoveTo(Vector3 position) => _nav.MoveTo(position);

    public Vector3 GetDestination() => _nav.GetDestination();
    public void Stop() => _nav.Stop();
    public bool HasReached => _nav.HasReached;

    public void FacePosition(Vector3 worldPosition)
    {
        Vector3 dir = Vector3.ProjectOnPlane(worldPosition - transform.position, Vector3.up);
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _turnSpeed * Time.deltaTime);
    }

    public void FireAt(Vector3 worldPosition)
    {
        _currentTarget = worldPosition;
        if (_isFiring || Time.time < _nextAttackTime) return;
        StartCoroutine(FireCoroutine());
    }

    private IEnumerator FireCoroutine()
    {
        _isFiring = true;
        _nextAttackTime = Time.time + _attackCooldown * CooldownMultiplier;

        yield return new WaitForSeconds(_preFirePause);

        float elapsed = 0f;
        while (elapsed < _burstDuration)
        {
            AudioSystem.Play(AudioSystem.Sound.EnemyGun);
            ShootProjectile(_currentTarget);
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
