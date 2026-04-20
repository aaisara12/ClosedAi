using System;
using System.Collections.Generic;
using System.Linq;

public static class StrategyPicker
{
    // Add new strategies here, ordered by priority (highest first preferred)
    private static readonly (int priority, Func<Strategy> create)[] _registry =
    {
        (15, () => new ChaseDownStrategy()),
        (12, () => new LeadingFireStrategy()),
        (10, () => new SuppressiveFireStrategy()),
        (8,  () => new CorneringAttackStrategy()),
        (1,  () => new CommanderStrategy()),
    };

    public static List<Strategy> Assign(List<EnemyAgent> pool)
    {
        var result = new List<Strategy>();
        var remaining = new List<EnemyAgent>(pool);

        foreach (var (_, create) in _registry.OrderByDescending(r => r.priority))
        {
            Strategy candidate;
            while (remaining.Count > 0 && (candidate = create()).CanFulfill(remaining))
            {
                var consumed = candidate.Consume(remaining);
                remaining.RemoveAll(consumed.Contains);
                result.Add(candidate);
            }
        }

        if (remaining.Count > 0)
        {
            var fallback = new DefaultStrategy();
            fallback.AssignAll(remaining);
            result.Add(fallback);
        }

        return result;
    }
}
