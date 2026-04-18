using System.Collections;
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

    private PlayerController _player;
    private Rigidbody _rb;
    private CameraController _camera;
    private float _currentFOV;
    private float _targetFOV;
    private bool _isDashing;

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
        if (_player.DashPressed() && _player.CanMove && !_isDashing)
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

        Vector3 dashDir = _cameraTransform.forward.normalized;
        Vector3 start = transform.position;

        // Stop short of any obstacle
        float safeDistance = _dashDistance;
        if (Physics.SphereCast(start, _player.ColliderRadius * 0.9f, dashDir, out RaycastHit hit, _dashDistance, _dashMask))
            safeDistance = Mathf.Max(0f, hit.distance - 0.1f);

        Vector3 target = start + dashDir * safeDistance;

        _targetFOV = _dashFOV;

        _rb.linearVelocity = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < _dashDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _dashDuration);
            _rb.MovePosition(Vector3.Lerp(start, target, t));
            yield return new WaitForFixedUpdate();
        }

        _rb.MovePosition(target);
        _rb.linearVelocity = dashDir * (_dashDistance / _dashDuration) * _momentumRetain;
        _rb.useGravity = true;

        _targetFOV = _camera != null ? _camera.BaseFOV : 90f;
        _isDashing = false;
        _player.CanMove = true;
    }
}
