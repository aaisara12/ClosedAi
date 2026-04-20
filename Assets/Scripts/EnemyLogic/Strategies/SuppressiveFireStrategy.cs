using UnityEngine;

public class SuppressiveFireStrategy : Strategy
{
    public override int Priority => 10;
    private int _losMask;
    public override StrategyRequirement[] Requirements => new[]
    {
        new StrategyRequirement { Type = EnemyType.Ranged, Count = 2 }
    };

    public override void OnStart()
    {
        Debug.Log("Starting suppressive fire strategy");


        _losMask = ~LayerMask.GetMask("Enemy");
        foreach (var a in _agents) 
            a.GetComponentInChildren<SignalStatus>()?.SetIcon(SignalIcon.Triangle);
    }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        foreach (var agent in _agents)
            if (agent is IShooter s) 
            {
                if (!HasLOS(agent.transform.position, playerPos))
                {
                    (agent as RangedAgent).MoveTo(playerPos);
                }
                (agent as IShooter).FacePosition(playerPos);
                s.FireAt(playerPos);
            }
    }

    // Offsets origin slightly toward target to clear the agent's own collider

    private bool HasLOS(Vector3 from, Vector3 to)
    {
        Vector3 origin = from + Vector3.up * 1.5f;
        Vector3 dir    = to - origin;
        return Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude, _losMask)
               && hit.collider.CompareTag("Player");
    }

    public override void End()
    {
        foreach (var a in _agents) 
            a.GetComponentInChildren<SignalStatus>()?.ResetIcon();
    }
}
