using UnityEngine;

public class SuppressiveFireStrategy : Strategy
{
    public override int Priority => 10;
    public override StrategyRequirement[] Requirements => new[]
    {
        new StrategyRequirement { Type = EnemyType.Ranged, Count = 3 }
    };

    public override void OnStart() { }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        if (!playerSpotted) return;
        foreach (var agent in _agents)
            if (agent is IShooter s) s.FireAt(playerPos);
    }

    public override void End() { }
}
