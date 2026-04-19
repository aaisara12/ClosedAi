using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class ChaseDownStrategy : Strategy
{
    public override int Priority => 15;
    public override StrategyRequirement[] Requirements => new[]
    {
        new StrategyRequirement { Type = EnemyType.Ranged, Count = 2 },
        new StrategyRequirement { Type = EnemyType.Melee,  Count = 2 },
    };

    private const float PositioningRadius = 8f;
    private const int   CandidateCount    = 6;

    private List<EnemyAgent> _meleeAgents;
    private List<EnemyAgent> _rangedAgents;
    private readonly Dictionary<EnemyAgent, Vector3?> _rangedTargets = new();

    public override void OnStart()
    {
        _meleeAgents  = _agents.Where(a => a.Type == EnemyType.Melee).ToList();
        _rangedAgents = _agents.Where(a => a.Type == EnemyType.Ranged).ToList();
        foreach (var a in _rangedAgents) _rangedTargets[a] = null;
    }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        foreach (var agent in _meleeAgents)
            (agent as IMovable)?.MoveTo(playerPos);

        foreach (var agent in _rangedAgents)
            UpdateRanged(agent, playerPos);
    }

    private void UpdateRanged(EnemyAgent agent, Vector3 playerPos)
    {
        var movable = agent as IMovable;
        var shooter  = agent as IShooter;

        if (!_rangedTargets[agent].HasValue)
        {
            Vector3 pos = SamplePosition(playerPos, agent.transform.position);
            _rangedTargets[agent] = pos;
            movable?.MoveTo(pos);
            return;
        }

        if (movable == null || !movable.HasReached) return;

        if (HasLOS(agent.transform.position, playerPos))
        {
            shooter?.FacePosition(playerPos);
            shooter?.FireAt(playerPos);
        }
        else
        {
            // Arrived but no LOS — pick a new position next tick
            _rangedTargets[agent] = null;
        }
    }

    private Vector3 SamplePosition(Vector3 playerPos, Vector3 fallback)
    {
        for (int i = 0; i < CandidateCount; i++)
        {
            float angle     = Random.Range(0f, 360f);
            Vector3 offset   = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * PositioningRadius;
            Vector3 candidate = playerPos + offset;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)
                && HasLOS(hit.position, playerPos))
                return hit.position;
        }
        return fallback;
    }

    // Offsets origin slightly toward target to clear the agent's own collider
    private bool HasLOS(Vector3 from, Vector3 to)
    {
        Vector3 dir  = to - from;
        float   dist = dir.magnitude;
        Vector3 origin = from + dir.normalized * 0.3f;
        return !Physics.Raycast(origin, dir.normalized, Mathf.Max(0f, dist - 0.3f));
    }

    public override void End()
    {
        foreach (var agent in _meleeAgents ?? new List<EnemyAgent>())
            (agent as IMovable)?.Stop();
        foreach (var agent in _rangedAgents ?? new List<EnemyAgent>())
            (agent as IMovable)?.Stop();
    }
}
