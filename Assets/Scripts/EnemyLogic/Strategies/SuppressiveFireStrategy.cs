using UnityEngine;

public class SuppressiveFireStrategy : Strategy
{
    public override int Priority => 10;
    public override StrategyRequirement[] Requirements => new[]
    {
        new StrategyRequirement { Type = EnemyType.Ranged, Count = 2 }
    };

    public override void OnStart() { }

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
        Vector3 dir  = to - from;
        float   dist = dir.magnitude;
        Vector3 origin = from + dir.normalized * 0.3f;
        return !Physics.Raycast(origin, dir.normalized, Mathf.Max(0f, dist - 0.3f));
    }

    public override void End() { }
}
