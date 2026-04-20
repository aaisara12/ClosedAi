using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class CorneringAttackStrategy : Strategy
{
    public override int Priority => 8;
    public override StrategyRequirement[] Requirements => new[]
    {
        new StrategyRequirement { Type = EnemyType.Melee, Count = 3 },
    };

    private const float FlankRadius = 6f;
    private const float FlankAngle  = 60f;

    private EnemyAgent _rusher;
    private readonly Dictionary<EnemyAgent, Vector3?> _flankTargets = new();

    public override void OnStart()
    {
        _rusher = _agents[0];
        foreach (var a in _agents) _flankTargets[a] = null;

        foreach (var a in _agents)
        {
            var s = a.GetComponentInChildren<SignalStatus>();
            s?.SetIcon(SignalIcon.RedCircle);
        }
    }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        EnemyAgent closest = ClosestAgentTo(playerPos);

        if (closest != _rusher)
        {
            _rusher = closest;
            foreach (var a in _agents)
                if (a != _rusher) _flankTargets[a] = null;
        }

        (_rusher as IMovable)?.MoveTo(playerPos);

        var flankers = _agents.Where(a => a != _rusher).ToList();
        foreach (var flanker in flankers)
        {
            if (flanker is not IMovable movable) continue;

            if (!_flankTargets[flanker].HasValue || movable.HasReached)
                _flankTargets[flanker] = PickFlankTarget(flanker, playerPos, flankers);

            movable.MoveTo(_flankTargets[flanker].Value);
        }
    }

    private EnemyAgent ClosestAgentTo(Vector3 pos)
    {
        EnemyAgent closest = _agents[0];
        float closestSqr = float.MaxValue;
        foreach (var a in _agents)
        {
            float sqr = (a.transform.position - pos).sqrMagnitude;
            if (sqr < closestSqr) { closestSqr = sqr; closest = a; }
        }
        return closest;
    }

    private Vector3 PickFlankTarget(EnemyAgent flanker, Vector3 playerPos, List<EnemyAgent> flankers)
    {
        Vector3 toPlayer = Vector3.ProjectOnPlane(playerPos - _rusher.transform.position, Vector3.up);
        if (toPlayer.sqrMagnitude < 0.01f) toPlayer = _rusher.transform.forward;
        toPlayer.Normalize();

        Vector3 candidateA = SampleNavMesh(playerPos + Quaternion.Euler(0f,  FlankAngle, 0f) * toPlayer * FlankRadius);
        Vector3 candidateB = SampleNavMesh(playerPos + Quaternion.Euler(0f, -FlankAngle, 0f) * toPlayer * FlankRadius);

        // If the other flanker already has a target, take the opposite candidate
        Vector3? otherTarget = flankers
            .Where(f => f != flanker && _flankTargets[f].HasValue)
            .Select(f => _flankTargets[f])
            .FirstOrDefault();

        if (otherTarget.HasValue)
        {
            float dA = Vector3.Distance(otherTarget.Value, candidateA);
            float dB = Vector3.Distance(otherTarget.Value, candidateB);
            return dA > dB ? candidateA : candidateB;
        }

        // No other target yet — pick whichever is closer to this flanker
        float toA = Vector3.Distance(flanker.transform.position, candidateA);
        float toB = Vector3.Distance(flanker.transform.position, candidateB);
        return toA < toB ? candidateA : candidateB;
    }

    private Vector3 SampleNavMesh(Vector3 candidate)
    {
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            return hit.position;
        return candidate;
    }

    public override void End()
    {
        foreach (var a in _agents)
            a.GetComponentInChildren<SignalStatus>()?.ResetIcon();
        foreach (var agent in _agents)
            (agent as IMovable)?.Stop();
    }
}
