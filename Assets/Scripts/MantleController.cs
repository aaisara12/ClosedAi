using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(Rigidbody), typeof(CapsuleCollider))]
public class MantleController : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _mantleReach = 1.2f;
    [SerializeField] private float _eyeHeight = 1.6f;
    [SerializeField] private float _minCheckHeight = 0.3f;
    [SerializeField] private int _checkSteps = 5;
    [SerializeField] private float _mantleDuration = 0.4f;
    [SerializeField] private float _mantleForwardStep = 0.5f;
    [SerializeField] private LayerMask _mantleMask = ~0;

    public bool IsMantling { get; private set; }

    private PlayerController _player;
    private Rigidbody _rb;
    private CapsuleCollider _col;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<CapsuleCollider>();
    }

    private void Update()
    {
        if (!IsMantling && _player.CanMove && _player.JumpPressed() && !_player.IsGrounded)
            TryMantle();
    }

    public bool TryMantle()
    {
        if (IsMantling) return false;
        if (!DetectLedge(out Vector3 standPoint)) return false;

        StartCoroutine(DoMantle(standPoint));
        return true;
    }

    private bool DetectLedge(out Vector3 standPoint)
    {
        standPoint = Vector3.zero;

        Vector3 forwardFlat = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
        float eyeWorldY = transform.position.y + _eyeHeight;

        for (int i = 0; i <= _checkSteps; i++)
        {
            float checkHeight = Mathf.Lerp(_minCheckHeight, _eyeHeight, (float)i / _checkSteps);
            Vector3 origin = transform.position + Vector3.up * checkHeight;

            if (!Physics.Raycast(origin, forwardFlat, out RaycastHit wallHit, _mantleReach, _mantleMask))
                continue;

            // Cast down from above eye level to find the ledge surface
            Vector3 castFrom = new Vector3(wallHit.point.x, eyeWorldY + 0.1f, wallHit.point.z) + forwardFlat * 0.05f;
            if (!Physics.Raycast(castFrom, Vector3.down, out RaycastHit ledgeHit, _eyeHeight + 0.2f, _mantleMask))
                continue;

            if (ledgeHit.point.y > eyeWorldY)
                continue;

            Vector3 capsuleBottom = ledgeHit.point + Vector3.up * _col.radius;
            Vector3 capsuleTop = ledgeHit.point + Vector3.up * (_player.StandHeight - _col.radius);
            if (Physics.CheckCapsule(capsuleBottom, capsuleTop, _col.radius * 0.9f, _mantleMask))
                continue;

            standPoint = ledgeHit.point;
            return true;
        }

        return false;
    }

    private IEnumerator DoMantle(Vector3 ledgeTop)
    {
        IsMantling = true;
        _player.CanMove = false;
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;

        Vector3 startPos = transform.position;
        Vector3 upPos = new Vector3(startPos.x, ledgeTop.y + _player.StandHeight * 0.5f, startPos.z);
        Vector3 forwardFlat = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
        Vector3 endPos = upPos + forwardFlat * _mantleForwardStep;

        float halfDuration = _mantleDuration * 0.5f;

        // Phase 1: lift up
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.fixedDeltaTime;
            _rb.MovePosition(Vector3.Lerp(startPos, upPos, Mathf.SmoothStep(0f, 1f, elapsed / halfDuration)));
            yield return new WaitForFixedUpdate();
        }

        // Phase 2: push forward
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.fixedDeltaTime;
            _rb.MovePosition(Vector3.Lerp(upPos, endPos, Mathf.SmoothStep(0f, 1f, elapsed / halfDuration)));
            yield return new WaitForFixedUpdate();
        }

        _rb.useGravity = true;
        _rb.linearVelocity = Vector3.zero;
        IsMantling = false;
        _player.CanMove = true;
    }
}
