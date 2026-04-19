using System;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class MeleeAttack : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _range = 2f;
    [SerializeField] private float _radius = 0.6f;
    [SerializeField] private float _cooldown = 0.5f;
    [SerializeField] private LayerMask _hitMask = ~0;

    public event Action OnAttacked;

    private PlayerController _player;
    private Pistol _pistol;
    private ClosedAi _input;
    private float _nextAttackTime;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _pistol = GetComponent<Pistol>();
        _input = new ClosedAi();
    }

    private void OnEnable() => _input.Player.Enable();
    private void OnDisable() => _input.Player.Disable();

    private void Update()
    {
        bool pistolActive = _pistol != null && _pistol.IsEquipped;
        if (_input.Player.Fire.WasPressedThisFrame() && !pistolActive && Time.time >= _nextAttackTime)
            DoMelee();
    }

    private void DoMelee()
    {
        _nextAttackTime = Time.time + _cooldown;
        OnAttacked?.Invoke();

        RaycastHit[] hits = Physics.SphereCastAll(
            _cameraTransform.position,
            _radius,
            _cameraTransform.forward,
            _range,
            _hitMask
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            OnMeleeHit(hit.collider, hit);
        }
    }

    private void OnMeleeHit(Collider col, RaycastHit hit)
    {
        if (col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // Implement damage logic here, e.g.:
            if (col.TryGetComponent(out Health health))
                health.TakeDamage(1);
        }   
    }
}
