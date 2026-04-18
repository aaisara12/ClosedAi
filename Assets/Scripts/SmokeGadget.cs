using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class SmokeGadget : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private GameObject _smokePrefab;
    [SerializeField] private float _throwForce = 12f;
    [SerializeField] private float _throwArcForce = 5f;
    [SerializeField] private float _cooldown = 8f;

    private ClosedAi _input;
    private float _nextThrowTime;

    private void Awake() => _input = new ClosedAi();
    private void OnEnable() => _input.Player.Enable();
    private void OnDisable() => _input.Player.Disable();

    private void Update()
    {
        if (_input.Player.Gadget.WasPressedThisFrame() && Time.time >= _nextThrowTime)
            Throw();
    }

    private void Throw()
    {
        if (_smokePrefab == null) return;

        _nextThrowTime = Time.time + _cooldown;

        GameObject obj = Instantiate(_smokePrefab, _cameraTransform.position + _cameraTransform.forward * 0.8f, _cameraTransform.rotation);
        if (obj.TryGetComponent(out Rigidbody rb))
            rb.AddForce(_cameraTransform.forward * _throwForce + Vector3.up * _throwArcForce, ForceMode.Impulse);
    }
}
