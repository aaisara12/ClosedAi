using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(Rigidbody))]
public class DashController : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _dashDistance = 8f;
    [SerializeField] private float _dashDuration = 0.15f;
    [SerializeField] private float _dashFOV = 110f;
    [SerializeField] private float _fovLerpSpeed = 12f;
    [SerializeField] private LayerMask _dashMask = ~0;
    [Range(0f, 1f)]
    [SerializeField] private float _momentumRetain = 0.25f;
    [SerializeField] private LayerMask _dashHitMask = ~0;
    [SerializeField] private float _dashCooldown = 1f;

    private PlayerController _player;
    private Rigidbody _rb;
    private CameraController _camera;
    private float _currentFOV;
    private float _targetFOV;
    private bool _isDashing;
    private float _nextDashTime;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody>();
        _camera = GetComponent<CameraController>();
    }

    private void Start()
    {
        _currentFOV = _camera != null ? _camera.BaseFOV : 90f;
        _targetFOV = _currentFOV;
    }

    private void Update()
    {
        if (_player.DashPressed() && _player.CanMove && !_isDashing && Time.time >= _nextDashTime)
            StartCoroutine(DoDash());

        if (_camera != null)
        {
            _currentFOV = Mathf.Lerp(_currentFOV, _targetFOV, _fovLerpSpeed * Time.deltaTime);
            _camera.SetFOV(_currentFOV);
        }
    }

    private IEnumerator DoDash()
    {
        _isDashing = true;
        _player.CanMove = false;
        _rb.useGravity = false;

        AudioSystem.Play(AudioSystem.Sound.Dash);

        Vector3 dashDir = _cameraTransform.forward.normalized;

        // Pull the cast origin back so the sphere never starts inside a wall the player
        // is already touching — SphereCast silently skips overlapping colliders at origin.
        float pullback = _player.ColliderRadius;
        float safeDistance = _dashDistance;
        if (Physics.SphereCast(transform.position - dashDir * pullback, _player.ColliderRadius * 0.9f, dashDir, out RaycastHit hit, _dashDistance + pullback, _dashMask))
            safeDistance = Mathf.Max(0f, hit.distance - pullback - 0.1f);

        _targetFOV = _dashFOV;

        float dashSpeed = safeDistance / _dashDuration;
        _rb.linearVelocity = dashDir * dashSpeed;

        var hitIds = new HashSet<int>();
        float elapsed = 0f;
        while (elapsed < _dashDuration)
        {
            elapsed += Time.fixedDeltaTime;

            Collider[] dashHits = Physics.OverlapSphere(transform.position, _player.ColliderRadius * 1.5f, _dashHitMask);
            foreach (Collider col in dashHits)
            {
                if (col.transform.IsChildOf(transform)) continue;
                if (!hitIds.Add(col.GetInstanceID())) continue;
                OnDashHit(col);
            }

            yield return new WaitForFixedUpdate();
        }

        _rb.linearVelocity = dashDir * dashSpeed * _momentumRetain;
        _rb.useGravity = true;

        _nextDashTime = Time.time + _dashCooldown;
        _targetFOV = _camera != null ? _camera.BaseFOV : 90f;
        _isDashing = false;
        _player.CanMove = true;
    }

    private void OnDashHit(Collider col)
    {
        // aisara => Only damage signals
        if (col.CompareTag("Signal"))
        {
            col.GetComponent<Health>()?.TakeDamage(1);
        }
    }
}
