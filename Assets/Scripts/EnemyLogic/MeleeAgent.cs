using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyNavigator), typeof(PlayerDetector))]
public class MeleeAgent : EnemyAgent, IMovable
{
    [Header("Attack")]
    [SerializeField] private float _attackCooldown = 2f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private Collider _attackCollider;

    [Header("Spin Animation")]
    [SerializeField] private Transform _spinObject;
    [SerializeField] private float _windupAngle = 30f;
    [SerializeField] private float _windupDuration = 0.12f;
    [SerializeField] private float _spinDuration = 0.28f;
    [SerializeField] private float _overshootAngle = 20f;
    [SerializeField] private float _recoveryDuration = 0.1f;

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

        Quaternion baseRot = _spinObject != null ? _spinObject.localRotation : Quaternion.identity;
        float angle = 0f;

        // Windup: rotate backwards
        float elapsed = 0f;
        while (elapsed < _windupDuration)
        {
            elapsed += Time.deltaTime;
            angle = Mathf.Lerp(0f, -_windupAngle, Mathf.SmoothStep(0f, 1f, elapsed / _windupDuration));
            ApplySpin(baseRot, angle);
            yield return null;
        }
        angle = -_windupAngle;

        // Spin: full 360 plus overshoot, collider active
        if (_attackCollider != null) _attackCollider.enabled = true;
        float spinFrom = angle;
        float spinTo   = 360f + _overshootAngle;
        elapsed = 0f;
        while (elapsed < _spinDuration)
        {
            elapsed += Time.deltaTime;
            angle = Mathf.Lerp(spinFrom, spinTo, Mathf.SmoothStep(0f, 1f, elapsed / _spinDuration));
            ApplySpin(baseRot, angle);
            yield return null;
        }
        angle = spinTo;
        if (_attackCollider != null) _attackCollider.enabled = false;

        // Recovery: ease back from overshoot to full 360 (= 0)
        float recoverFrom = angle;
        elapsed = 0f;
        while (elapsed < _recoveryDuration)
        {
            elapsed += Time.deltaTime;
            angle = Mathf.Lerp(recoverFrom, 360f, Mathf.SmoothStep(0f, 1f, elapsed / _recoveryDuration));
            ApplySpin(baseRot, angle);
            yield return null;
        }

        if (_spinObject != null) _spinObject.localRotation = baseRot;
        _isAttacking = false;
    }

    private void ApplySpin(Quaternion baseRotation, float angle)
    {
        if (_spinObject == null) return;
        _spinObject.localRotation = baseRotation * Quaternion.AngleAxis(angle, Vector3.up);
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
