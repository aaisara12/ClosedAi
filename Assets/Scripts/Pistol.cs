using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class Pistol : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private GameObject _pistolModel;
    [SerializeField] private float _projectileSpeed = 40f;
    [SerializeField] private float _reloadTime = 1.5f;

    public bool IsEquipped { get; private set; }
    public bool IsLoaded { get; private set; } = true;

    private ClosedAi _input;
    private Coroutine _reloadCoroutine;

    private void Awake()
    {
        _input = new ClosedAi();
        SetModelActive(false);
    }

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
        SetModelActive(IsEquipped);

        if (!IsEquipped && _reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }
    }

    private void Shoot()
    {
        if (_projectilePrefab == null) return;

        IsLoaded = false;

        GameObject proj = Instantiate(_projectilePrefab, _cameraTransform.position + _cameraTransform.forward * 0.8f, _cameraTransform.rotation);
        if (proj.TryGetComponent(out Rigidbody rb))
            rb.linearVelocity = _cameraTransform.forward * _projectileSpeed;

        _reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        // Trigger reload animation here when imported
        yield return new WaitForSeconds(_reloadTime);
        IsLoaded = true;
        _reloadCoroutine = null;
    }

    private void SetModelActive(bool active)
    {
        if (_pistolModel != null)
            _pistolModel.SetActive(active);
    }
}
