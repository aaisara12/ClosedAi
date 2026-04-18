using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(Rigidbody))]
public class GrappleController : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _grappleRange = 30f;
    [SerializeField] private float _hookTravelSpeed = 40f;
    [SerializeField] private float _pullSpeed = 18f;
    [SerializeField] private float _arriveDistance = 1.8f;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private LayerMask _grappleMask = ~0;

    private PlayerController _player;
    private Rigidbody _rb;
    private MantleController _mantler;

    private bool _isActive;
    private bool _hookTraveling;
    private bool _hasTarget;
    private Vector3 _grapplePoint;
    private Vector3 _hookPos;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody>();
        _mantler = GetComponent<MantleController>();
    }

    private void Update()
    {
        if (_player.GrapplePressed())
        {
            if (!_isActive)
                TryFire();
            else
                Cancel(preserveMomentum: true);
        }

        if (_hookTraveling)
            AdvanceHook();

        UpdateLine();
    }

    private void FixedUpdate()
    {
        if (!_isActive || _hookTraveling || !_hasTarget) return;

        Vector3 toTarget = _grapplePoint - transform.position;
        float dist = toTarget.magnitude;

        if (dist <= _arriveDistance)
        {
            Arrive();
            return;
        }

        _rb.linearVelocity = toTarget.normalized * _pullSpeed;
    }

    private void TryFire()
    {
        _hasTarget = Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, _grappleRange, _grappleMask);
        _grapplePoint = _hasTarget ? hit.point : _cameraTransform.position + _cameraTransform.forward * _grappleRange;
        _hookPos = _cameraTransform.position;
        _hookTraveling = true;
        _isActive = true;

        if (_hasTarget)
        {
            _player.CanMove = false;
            _rb.useGravity = false;
        }

        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = 2;
        }
    }

    private void AdvanceHook()
    {
        _hookPos = Vector3.MoveTowards(_hookPos, _grapplePoint, _hookTravelSpeed * Time.deltaTime);
        if (Vector3.Distance(_hookPos, _grapplePoint) < 0.05f)
        {
            _hookPos = _grapplePoint;
            _hookTraveling = false;

            if (!_hasTarget)
                Cancel(preserveMomentum: true);
        }
    }

    private void Arrive()
    {
        _isActive = false;
        _hookTraveling = false;
        _rb.useGravity = true;

        if (_lineRenderer != null) _lineRenderer.enabled = false;

        // Attempt mantle; if it fails, restore movement so player doesn't get stuck
        if (!_mantler.TryMantle())
        {
            _player.CanMove = true;
        }
    }

    private void Cancel(bool preserveMomentum)
    {
        _isActive = false;
        _hookTraveling = false;
        _rb.useGravity = true;
        _player.CanMove = true;

        if (!preserveMomentum)
            _rb.linearVelocity = Vector3.zero;

        if (_lineRenderer != null) _lineRenderer.enabled = false;
    }

    private void UpdateLine()
    {
        if (_lineRenderer == null || !_isActive) return;
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, _hookPos);
    }
}
