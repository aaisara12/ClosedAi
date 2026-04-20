using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class Pistol : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 40f;
    [SerializeField] private float _reloadTime = 1.5f;
    [SerializeField] private float _maxRange = 200f;
    [SerializeField] private LayerMask _aimMask = ~0;

    public event Action<bool> OnEquipChanged;
    public event Action OnFired;
    public event Action OnReloadStarted;

    public bool IsEquipped { get; private set; }
    public bool IsLoaded { get; private set; } = true;
    public float ReloadTime => _reloadTime;

    private ClosedAi _input;
    private Coroutine _reloadCoroutine;

    private void Awake() => _input = new ClosedAi();

    private void OnEnable() => _input.Player.Enable();
    private void OnDisable() => _input.Player.Disable();

    private void Update()
    {
        if (_input.Player.Swap.WasPressedThisFrame())
            ToggleEquip();

        if (IsEquipped && IsLoaded && _input.Player.Fire.WasPressedThisFrame())
            Shoot();
    }

    private void ToggleEquip()
    {
        IsEquipped = !IsEquipped;
        OnEquipChanged?.Invoke(IsEquipped);

        if (!IsEquipped && _reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }
        else if (IsEquipped && !IsLoaded)
        {
            _reloadCoroutine = StartCoroutine(ReloadCoroutine());
        }
    }

    private void Shoot()
    {
        if (_projectilePrefab == null) return;

        IsLoaded = false;
        OnFired?.Invoke();

        Vector3 aimTarget = Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, _maxRange, _aimMask)
            ? hit.point
            : _cameraTransform.position + _cameraTransform.forward * _maxRange;

        Transform origin = _firePoint ?? _cameraTransform;
        Vector3 dir = (aimTarget - origin.position).normalized;

        GameObject proj = Instantiate(_projectilePrefab, origin.position, Quaternion.LookRotation(dir));
        if (proj.TryGetComponent(out Rigidbody rb))
            rb.linearVelocity = dir * _projectileSpeed;

        _reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        OnReloadStarted?.Invoke();
        yield return new WaitForSeconds(_reloadTime);
        IsLoaded = true;
        _reloadCoroutine = null;
    }
}
