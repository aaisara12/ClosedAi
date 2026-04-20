using System.Collections.Generic;
// using System.Diagnostics;
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
    private const float MinSeparation     = 2f;

    private static readonly int SampleMask = LayerMask.GetMask("Player", "Terrain");

    private List<EnemyAgent> _meleeAgents;
    private List<EnemyAgent> _rangedAgents;
    private readonly Dictionary<EnemyAgent, Vector3?> _rangedTargets = new();

    public override void OnStart()
    {
        _meleeAgents  = _agents.Where(a => a.Type == EnemyType.Melee).ToList();
        _rangedAgents = _agents.Where(a => a.Type == EnemyType.Ranged).ToList();
        foreach (var a in _rangedAgents) _rangedTargets[a] = null;

        foreach (var a in _agents)
            a.GetComponentInChildren<SignalStatus>()?.SetIcon(SignalIcon.Triangle);
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
            var claimed = _rangedTargets
                .Where(kvp => kvp.Key != agent && kvp.Value.HasValue)
                .Select(kvp => kvp.Value.Value);
            Vector3 pos = SamplePosition(playerPos, agent.transform.position, claimed);
            _rangedTargets[agent] = pos;
            movable?.MoveTo(pos);
            return;
        }

        if (movable == null || !movable.HasReached) return;

        shooter?.FacePosition(playerPos);
        shooter?.FireAt(playerPos);
    }

    // Shoots evenly spaced rays outward from the player. A ray miss means open space at
    // full radius; a hit is valid if it's far enough that the agent has room to stand and shoot.
    private Vector3 SamplePosition(Vector3 playerPos, Vector3 fallback, IEnumerable<Vector3> claimed)
    {
        for (int i = 0; i < CandidateCount; i++)
        {
            float   angle = 360f * i / CandidateCount;
            Vector3 dir   = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            Vector3 candidate;
            if (Physics.Raycast(playerPos, dir, out RaycastHit hit, PositioningRadius, SampleMask))
            {
                if (hit.distance < PositioningRadius * 0.5f) continue;
                candidate = hit.point - dir * 0.2f;
            }
            else
            {
                candidate = playerPos + dir * PositioningRadius;
            }

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                continue;

            bool tooClose = false;
            foreach (var pos in claimed)
                if (Vector3.Distance(navHit.position, pos) < MinSeparation)
                    { tooClose = true; break; }

            if (!tooClose) return navHit.position;
        }
        return fallback;
    }

    public override void End()
    {
        foreach (var a in _agents)
            a.GetComponentInChildren<SignalStatus>()?.ResetIcon();

        foreach (var agent in _meleeAgents ?? new List<EnemyAgent>())
            (agent as IMovable)?.Stop();
        foreach (var agent in _rangedAgents ?? new List<EnemyAgent>())
            (agent as IMovable)?.Stop();
    }
}
