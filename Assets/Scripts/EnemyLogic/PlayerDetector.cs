using UnityEngine;

[RequireComponent(typeof(EnemyAgent))]
public class PlayerDetector : MonoBehaviour
{
    [SerializeField] private float _detectionRange = 15f;
    [SerializeField] [Range(1f, 180f)] private float _horizontalAngle = 60f;
    [SerializeField] [Range(1f, 180f)] private float _verticalAngle = 30f;
    [SerializeField] private int _horizontalRayCount = 7;
    [SerializeField] private int _verticalRayCount = 3;
    [SerializeField] private float _checkInterval = 0.5f;
    [SerializeField] private Transform _eyeTransform;
    [SerializeField] private LayerMask _sightMask = ~0;

    private EnemyAgent _agent;
    private float _nextCheckTime;

    private void Awake() => _agent = GetComponent<EnemyAgent>();

    private void Update()
    {
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + _checkInterval;

        if (TryDetectPlayer(out Vector3 playerPos))
            _agent.Brain?.ReportPlayerSpotted(playerPos);
    }

    private bool TryDetectPlayer(out Vector3 playerPos)
    {
        if (TryConeSweep(out playerPos)) return true;

        // During confirming, also check direct LOS to the last known position
        // so the player can't shake detection simply by stepping out of the cone
        var brain = _agent.Brain;
        if (brain != null && (brain.IsConfirming || brain.IsExecuting))
            return TryDirectLOS(PlayerController.Instance.transform.position, out playerPos);

        return false;
    }

    private bool TryConeSweep(out Vector3 playerPos)
    {
        playerPos = Vector3.zero;
        Vector3 origin = _eyeTransform != null ? _eyeTransform.position : transform.position;

        for (int v = 0; v < _verticalRayCount; v++)
        {
            float vt    = _verticalRayCount == 1 ? 0.5f : (float)v / (_verticalRayCount - 1);
            float pitch = Mathf.Lerp(-_verticalAngle * 0.5f, _verticalAngle * 0.5f, vt);

            for (int h = 0; h < _horizontalRayCount; h++)
            {
                float ht  = _horizontalRayCount == 1 ? 0.5f : (float)h / (_horizontalRayCount - 1);
                float yaw = Mathf.Lerp(-_horizontalAngle * 0.5f, _horizontalAngle * 0.5f, ht);

                Vector3 dir = transform.TransformDirection(Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward);

                if (Physics.Raycast(origin, dir, out RaycastHit hit, _detectionRange, _sightMask)
                    && hit.collider.CompareTag("Player"))
                {
                    playerPos = hit.collider.transform.position;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryDirectLOS(Vector3 targetPos, out Vector3 playerPos)
    {
        playerPos = Vector3.zero;
        Vector3 origin = _eyeTransform != null ? _eyeTransform.position : transform.position;
        Vector3 dir    = targetPos - origin;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, _detectionRange, _sightMask)
            && hit.collider.CompareTag("Player"))
        {
            playerPos = hit.collider.transform.position;
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = _eyeTransform != null ? _eyeTransform.position : transform.position;
        Gizmos.color = Color.yellow;

        int segments = 20;
        float halfH  = _horizontalAngle * 0.5f;
        float halfV  = _verticalAngle   * 0.5f;

        foreach (float pitch in new[] { -halfV, 0f, halfV })
        {
            Vector3 prev = origin + Quaternion.Euler(pitch, -halfH, 0f) * transform.forward * _detectionRange;
            Gizmos.DrawLine(origin, prev);
            for (int i = 1; i <= segments; i++)
            {
                float   yaw  = Mathf.Lerp(-halfH, halfH, (float)i / segments);
                Vector3 next = origin + transform.TransformDirection(Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward) * _detectionRange;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
            Gizmos.DrawLine(origin, prev);
        }

        foreach (float yaw in new[] { -halfH, 0f, halfH })
        {
            Vector3 prev = origin + Quaternion.Euler(-halfV, yaw, 0f) * transform.forward * _detectionRange;
            for (int i = 1; i <= segments; i++)
            {
                float   pitch = Mathf.Lerp(-halfV, halfV, (float)i / segments);
                Vector3 next  = origin + transform.TransformDirection(Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward) * _detectionRange;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
