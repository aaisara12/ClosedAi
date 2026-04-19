using UnityEngine;

[RequireComponent(typeof(EnemyAgent))]
public class PlayerDetector : MonoBehaviour
{
    [SerializeField] private float _detectionRange = 15f;
    [SerializeField] [Range(1f, 180f)] private float _detectionAngle = 60f;
    [SerializeField] private int _rayCount = 7;
    [SerializeField] private float _checkInterval = 0.15f;
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
        playerPos = Vector3.zero;
        Vector3 origin = _eyeTransform != null ? _eyeTransform.position : transform.position;

        for (int i = 0; i < _rayCount; i++)
        {
            float t = _rayCount == 1 ? 0.5f : (float)i / (_rayCount - 1);
            float angle = Mathf.Lerp(-_detectionAngle * 0.5f, _detectionAngle * 0.5f, t);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, _detectionRange, _sightMask)
                && hit.collider.CompareTag("Player"))
            {
                playerPos = hit.collider.transform.position;
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = _eyeTransform != null ? _eyeTransform.position : transform.position;
        Gizmos.color = Color.yellow;

        int arcSegments = 20;
        float halfAngle = _detectionAngle * 0.5f;
        Vector3 prev = origin + Quaternion.Euler(0f, -halfAngle, 0f) * transform.forward * _detectionRange;

        Gizmos.DrawLine(origin, prev);
        for (int i = 1; i <= arcSegments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / arcSegments);
            Vector3 next = origin + Quaternion.Euler(0f, angle, 0f) * transform.forward * _detectionRange;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        Gizmos.DrawLine(origin, prev);
    }
}
