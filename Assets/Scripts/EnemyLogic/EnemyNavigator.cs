using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavigator : MonoBehaviour, IMovable
{
    private NavMeshAgent _nav;

    private void Awake() => _nav = GetComponent<NavMeshAgent>();

    public void MoveTo(Vector3 position) => _nav.SetDestination(position);
    public Vector3 GetDestination() => _nav.destination;

    public void Stop()
    {
        if (_nav.isOnNavMesh) _nav.ResetPath();
        _nav.velocity = Vector3.zero;
    }

    public bool HasReached => !_nav.pathPending && _nav.remainingDistance <= _nav.stoppingDistance;
}
