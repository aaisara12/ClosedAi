using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class Pistol : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 40f;
    [SerializeField] private float _reloadTime = 1.5f;

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

        GameObject proj = Instantiate(_projectilePrefab, _cameraTransform.position + _cameraTransform.forward * 0.8f, _cameraTransform.rotation);
        if (proj.TryGetComponent(out Rigidbody rb))
            rb.linearVelocity = _cameraTransform.forward * _projectileSpeed;

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
