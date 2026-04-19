using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct StrategyRequirement
{
    public EnemyType Type;
    public int Count;
}

public abstract class Strategy
{
    public abstract int Priority { get; }
    public abstract StrategyRequirement[] Requirements { get; }

    protected List<EnemyAgent> _agents = new();

    public bool CanFulfill(IReadOnlyList<EnemyAgent> pool)
    {
        foreach (var req in Requirements)
            if (pool.Count(a => a.Type == req.Type) < req.Count) return false;
        return true;
    }

    // Pulls required agents from pool, assigns them, and calls OnStart. Returns consumed agents.
    public List<EnemyAgent> Consume(List<EnemyAgent> pool)
    {
        var consumed = new List<EnemyAgent>();
        foreach (var req in Requirements)
            consumed.AddRange(pool.Where(a => a.Type == req.Type && !consumed.Contains(a)).Take(req.Count));
        _agents = consumed;
        OnStart();
        return consumed;
    }

    public abstract void OnStart();
    public abstract void Tick(Vector3 playerPos, bool playerSpotted);
    public abstract void End();
}
