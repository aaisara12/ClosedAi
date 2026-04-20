using System.Collections.Generic;
using UnityEngine;

public class LeadingFireStrategy : Strategy
{
    public override int Priority => 12;
    public override StrategyRequirement[] Requirements => new[]
    {
        new StrategyRequirement { Type = EnemyType.Ranged, Count = 2 },
    };

    private const float LightLeadTime  = 0.1f;
    private const float HeavyLeadTime  = 0.3f;
    private const int   HistoryFrames  = 6;

    private EnemyAgent _lightLeader;
    private EnemyAgent _heavyLeader;
    private readonly Queue<(Vector3 pos, float time)> _posHistory = new();
    private int _losMask;

    public override void OnStart()
    {
        _lightLeader = _agents[0];
        _heavyLeader = _agents[1];
        _losMask = ~LayerMask.GetMask("Enemy");
    }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        _posHistory.Enqueue((playerPos, Time.time));
        while (_posHistory.Count > HistoryFrames)
            _posHistory.Dequeue();

        TickAgent(_lightLeader, playerPos, LightLeadTime);
        TickAgent(_heavyLeader, playerPos, HeavyLeadTime);
    }

    private void TickAgent(EnemyAgent agent, Vector3 playerPos, float leadTime)
    {
        var movable = agent as IMovable;
        var shooter  = agent as IShooter;

        if (HasLOS(agent, playerPos))
        {
            movable?.Stop();
            Vector3 aimPos = PredictedPosition(playerPos, leadTime);
            shooter?.FacePosition(aimPos);
            shooter?.FireAt(aimPos);
        }
        else
        {
            movable?.MoveTo(playerPos);
        }
    }

    private bool HasLOS(EnemyAgent agent, Vector3 playerPos)
    {
        Vector3 origin = agent.transform.position + Vector3.up * 1.5f;
        Vector3 dir    = playerPos - origin;
        return Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude, _losMask)
               && hit.collider.CompareTag("Player");
    }

    // Estimates where the player will be in leadTime seconds using average velocity
    // across the stored position history.
    private Vector3 PredictedPosition(Vector3 currentPos, float leadTime)
    {
        if (_posHistory.Count < 2) return currentPos;
        var oldest = _posHistory.Peek();
        float elapsed = Time.time - oldest.time;
        if (elapsed < 0.001f) return currentPos;
        Vector3 velocity = (currentPos - oldest.pos) / elapsed;
        return currentPos + velocity * leadTime;
    }

    public override void End()
    {
        foreach (var agent in _agents)
            (agent as IMovable)?.Stop();
    }
}
