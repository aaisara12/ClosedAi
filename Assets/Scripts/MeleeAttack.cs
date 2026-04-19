using System;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class MeleeAttack : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Vector3 _boxHalfExtents = new Vector3(0.6f, 0.8f, 0.8f);
    [SerializeField] private float _boxOffset = 0.8f;
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

        Vector3 center = _cameraTransform.position + _cameraTransform.forward * _boxOffset;
        Collider[] hits = Physics.OverlapBox(center, _boxHalfExtents, _cameraTransform.rotation, _hitMask);

        foreach (Collider col in hits)
        {
            if (col.transform.IsChildOf(transform)) continue;
            OnMeleeHit(col);
        }
    }

    private void OnMeleeHit(Collider col)
    {
        if (col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            if (col.TryGetComponent(out Health health))
                health.TakeDamage(1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_cameraTransform == null) return;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.matrix = Matrix4x4.TRS(
            _cameraTransform.position + _cameraTransform.forward * _boxOffset,
            _cameraTransform.rotation,
            Vector3.one
        );
        Gizmos.DrawWireCube(Vector3.zero, _boxHalfExtents * 2f);
    }
}
