using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyNavigator), typeof(PlayerDetector))]
public class MeleeAgent : EnemyAgent, IMovable
{
    [Header("Attack")]
    [SerializeField] private float _attackCooldown = 2f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private Vector3 _hitBoxHalfExtents = new Vector3(1.5f, 0.8f, 1.5f);
    [SerializeField] private LayerMask _hitMask = ~0;

    [Header("Spin Animation")]
    [SerializeField] private Transform _spinObject;
    [SerializeField] private float _windupAngle = 30f;
    [SerializeField] private float _windupDuration = 0.12f;
    [SerializeField] private float _spinDuration = 0.28f;
    [SerializeField] private float _overshootAngle = 20f;
    [SerializeField] private float _recoveryDuration = 0.1f;

    [Header("Attack Lunge")]
    [SerializeField] private float _lungeDistance = 2f;
    [SerializeField] private AnimationCurve _lungeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

        // Spin: full 360 plus overshoot, box hit detection active
        // Spin: full 360 plus overshoot, box hit detection active + forward lunge
        float spinFrom = angle;
        float spinTo   = 360f + _overshootAngle;
        elapsed = 0f;
        var hitIds = new HashSet<int>();
        Vector3 lungeStart = transform.position;
        Vector3 lungeEnd   = transform.position + transform.forward * _lungeDistance;
        while (elapsed < _spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _spinDuration);
            angle = Mathf.Lerp(spinFrom, spinTo, t);
            ApplySpin(baseRot, angle);

            // Move forward along the lunge arc
            transform.position = Vector3.Lerp(lungeStart, lungeEnd, _lungeCurve.Evaluate(t));

            Collider[] hits = Physics.OverlapBox(transform.position, _hitBoxHalfExtents, Quaternion.identity, _hitMask);
            foreach (Collider col in hits)
            {
                if (col.transform.IsChildOf(transform)) continue;
                if (!hitIds.Add(col.GetInstanceID())) continue;
                OnAttackHit(col);
            }

            yield return null;
        }
        angle = spinTo;

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

    private void OnAttackHit(Collider col)
    {
        if (col.CompareTag("Player"))
            Debug.Log($"{name} hit the player");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, _hitBoxHalfExtents * 2f);
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
