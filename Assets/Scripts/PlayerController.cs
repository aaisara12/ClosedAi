using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Ground Movement")]
    [SerializeField] private float _moveSpeed = 6f;
    [SerializeField] private float _crouchSpeed = 3f;
    [SerializeField] private float _groundAccel = 20f;
    [SerializeField] private float _groundDecel = 15f;

    [Header("Air Movement")]
    [SerializeField] private float _airSteering = 4f;
    [SerializeField] private float _maxAirSpeed = 6f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 8f;

    [Header("Crouch")]
    [SerializeField] private float _crouchHeight = 1f;
    [SerializeField] private float _crouchCameraDropAmount = 0.6f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask _groundMask = ~0;

    public bool IsGrounded { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool CanMove { get; set; } = true;

    public float ColliderRadius => _col.radius;
    public float StandHeight => _standHeight;
    public Vector3 Velocity => _rb.linearVelocity;
    public void SetVelocity(Vector3 v) => _rb.linearVelocity = v;
    public void AddImpulse(Vector3 impulse) => _rb.AddForce(impulse, ForceMode.Impulse);

    private Rigidbody _rb;
    private CapsuleCollider _col;
    private CameraController _cameraController;
    private ClosedAi _input;
    private float _standHeight;
    private Vector3 _standCenter;
    private float _standBottom;

    public static PlayerController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<CapsuleCollider>();
        _cameraController = GetComponent<CameraController>();
        _input = new ClosedAi();

        _standHeight = _col.height;
        _standCenter = _col.center;
        _standBottom = _standCenter.y - _standHeight * 0.5f;

        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void OnEnable() => _input.Player.Enable();
    private void OnDisable() => _input.Player.Disable();

    private void Update()
    {
        IsGrounded = CheckGround();
        HandleCrouch();

        if (IsGrounded && CanMove && JumpPressed())
            Jump();
    }

    private void FixedUpdate()
    {
        if (!CanMove) return;
        if (IsGrounded)
            ApplyGroundMovement();
        else
            ApplyAirSteering();
    }

    private bool CheckGround()
    {
        float checkRadius = _col.radius * 0.9f;
        Vector3 origin = transform.position + _col.center + Vector3.down * (_col.height * 0.5f - _col.radius);
        return Physics.SphereCast(origin, checkRadius, Vector3.down, out _, 0.15f, _groundMask, QueryTriggerInteraction.Ignore);
    }

    private void HandleCrouch()
    {
        bool wants = _input.Player.Crouch.IsPressed();

        if (wants && !IsCrouching)
        {
            IsCrouching = true;
            _col.height = _crouchHeight;
            _col.center = new Vector3(_standCenter.x, _standBottom + _crouchHeight * 0.5f, _standCenter.z);
            if (_cameraController != null) _cameraController.CameraHeightOffset = -_crouchCameraDropAmount;
        }
        else if (!wants && IsCrouching && CanStandUp())
        {
            IsCrouching = false;
            _col.height = _standHeight;
            _col.center = _standCenter;
            if (_cameraController != null) _cameraController.CameraHeightOffset = 0f;
        }
    }

    private bool CanStandUp()
    {
        Vector3 top = transform.position + _standCenter + Vector3.up * (_standHeight * 0.5f - _col.radius);
        return !Physics.SphereCast(top, _col.radius, Vector3.up, out _,
            _standHeight - _crouchHeight, _groundMask, QueryTriggerInteraction.Ignore);
    }

    private void Jump()
    {
        Vector3 v = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(v.x, _jumpForce, v.z);
    }

    private void ApplyGroundMovement()
    {
        Vector2 input = _input.Player.Move.ReadValue<Vector2>();
        bool hasInput = input.magnitude > 0.01f;

        float speed = IsCrouching ? _crouchSpeed : _moveSpeed;
        Vector3 targetXZ = hasInput
            ? (transform.right * input.x + transform.forward * input.y).normalized * speed
            : Vector3.zero;

        Vector3 currentXZ = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        float rate = hasInput ? _groundAccel : _groundDecel;
        Vector3 newXZ = Vector3.MoveTowards(currentXZ, targetXZ, rate * Time.fixedDeltaTime);

        _rb.linearVelocity = new Vector3(newXZ.x, _rb.linearVelocity.y, newXZ.z);
    }

    private void ApplyAirSteering()
    {
        Vector2 input = _input.Player.Move.ReadValue<Vector2>();
        if (input.magnitude < 0.01f) return;

        Vector3 dir = (transform.right * input.x + transform.forward * input.y).normalized;
        _rb.AddForce(dir * _airSteering, ForceMode.Acceleration);

        Vector3 xz = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (xz.magnitude > _maxAirSpeed)
        {
            xz = xz.normalized * _maxAirSpeed;
            _rb.linearVelocity = new Vector3(xz.x, _rb.linearVelocity.y, xz.z);
        }
    }

    public bool JumpPressed() => _input.Player.Jump.WasPressedThisFrame();
    public bool DashPressed() => _input.Player.Dash.WasPressedThisFrame();
    public bool GrapplePressed() => _input.Player.Grapple.WasPressedThisFrame();
    public Vector2 MoveInput() => _input.Player.Move.ReadValue<Vector2>();
}
