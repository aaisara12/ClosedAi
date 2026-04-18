using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(Rigidbody))]
public class WallRunController : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float _wallCheckDistance = 0.7f;
    [SerializeField] private float _wallNormalMaxUpDot = 0.3f; // Rejects near-horizontal surfaces
    [SerializeField] private LayerMask _wallMask = ~0;

    [Header("Wall Run")]
    [SerializeField] private float _wallRunSpeed = 8f;
    [SerializeField] private float _wallGravity = 4f;
    [SerializeField] private float _maxWallFallSpeed = 4f;
    [SerializeField] private float _detachLookAngle = 50f; // Degrees from wall before detaching

    [Header("Wall Jump")]
    [SerializeField] private float _wallJumpUpForce = 7f;
    [SerializeField] private float _wallJumpAwayForce = 9f;
    [SerializeField] private float _wallJumpMinAngleFromWall = 15f; // Min degrees away from wall surface

    [Header("Camera")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _cameraRollAngle = 12f;
    [SerializeField] private float _cameraRollSpeed = 8f;

    public bool IsWallRunning { get; private set; }

    private PlayerController _player;
    private Rigidbody _rb;
    private CameraController _camera;

    private Vector3 _wallNormal;
    private float _targetRoll;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody>();
        _camera = GetComponent<CameraController>();
    }

    private void Update()
    {
        if (!IsWallRunning)
        {
            if (!_player.IsGrounded && _player.CanMove && _player.JumpPressed())
            {
                if (DetectWall(out Vector3 normal, out int side))
                    EnterWallRun(normal, side);
            }
        }
        else
        {
            if (!DetectWall(out Vector3 updatedNormal, out _))
            {
                ExitWallRun();
            }
            else
            {
                _wallNormal = updatedNormal;
                if (IsLookTooFarFromWall())
                    ExitWallRun();
                else if (_player.JumpPressed())
                    DoWallJump();
            }
        }

        SmoothCameraRoll();
    }

    private void FixedUpdate()
    {
        if (!IsWallRunning) return;

        Vector3 wallForward = GetWallForward();
        float yVel = Mathf.Max(_rb.linearVelocity.y - _wallGravity * Time.fixedDeltaTime, -_maxWallFallSpeed);
        _rb.linearVelocity = new Vector3(wallForward.x * _wallRunSpeed, yVel, wallForward.z * _wallRunSpeed);
    }

    private bool DetectWall(out Vector3 normal, out int side)
    {
        if (Physics.Raycast(transform.position, transform.right, out RaycastHit hitR, _wallCheckDistance, _wallMask)
            && Mathf.Abs(Vector3.Dot(hitR.normal, Vector3.up)) < _wallNormalMaxUpDot)
        {
            normal = hitR.normal;
            side = 1;
            return true;
        }

        if (Physics.Raycast(transform.position, -transform.right, out RaycastHit hitL, _wallCheckDistance, _wallMask)
            && Mathf.Abs(Vector3.Dot(hitL.normal, Vector3.up)) < _wallNormalMaxUpDot)
        {
            normal = hitL.normal;
            side = -1;
            return true;
        }

        normal = Vector3.zero;
        side = 0;
        return false;
    }

    private void EnterWallRun(Vector3 wallNormal, int side)
    {
        IsWallRunning = true;
        _player.CanMove = false;
        _wallNormal = wallNormal;
        _rb.useGravity = false;

        Vector3 v = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(v.x, 0f, v.z);

        _targetRoll = side * _cameraRollAngle;
    }

    private void ExitWallRun()
    {
        IsWallRunning = false;
        _player.CanMove = true;
        _rb.useGravity = true;
        _targetRoll = 0f;
    }

    private void DoWallJump()
    {
        Vector3 lookFlat = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;

        float maxAngleFromNormal = 90f - _wallJumpMinAngleFromWall;
        float angleFromNormal = Vector3.Angle(lookFlat, _wallNormal);

        if (angleFromNormal > maxAngleFromNormal)
        {
            float overshoot = angleFromNormal - maxAngleFromNormal;
            lookFlat = Vector3.RotateTowards(lookFlat, _wallNormal, overshoot * Mathf.Deg2Rad, 0f).normalized;
        }

        _rb.linearVelocity = lookFlat * _wallJumpAwayForce + Vector3.up * _wallJumpUpForce;
        ExitWallRun();
    }

    private bool IsLookTooFarFromWall()
    {
        Vector3 wallForward = GetWallForward();
        Vector3 lookFlat = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
        float angle = Mathf.Min(Vector3.Angle(wallForward, lookFlat), Vector3.Angle(-wallForward, lookFlat));
        return angle > _detachLookAngle;
    }

    private Vector3 GetWallForward()
    {
        Vector3 wallForward = Vector3.Cross(_wallNormal, Vector3.up).normalized;
        if (Vector3.Dot(wallForward, transform.forward) < 0f)
            wallForward = -wallForward;
        return wallForward;
    }

    private void SmoothCameraRoll()
    {
        if (_camera == null) return;
        _camera.CameraRoll = Mathf.Lerp(_camera.CameraRoll, _targetRoll, _cameraRollSpeed * Time.deltaTime);
    }
}
