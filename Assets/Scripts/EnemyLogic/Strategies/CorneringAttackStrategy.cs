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

    private enum Phase { Positioning, Rushing }
    private Phase _phase;

    private EnemyAgent       _lead;
    private List<EnemyAgent> _flankers;
    private Vector3          _flankTargetA, _flankTargetB;
    private bool             _targetsSet;

    public override void OnStart()
    {
        _lead      = _agents[0];
        _flankers  = _agents.Skip(1).ToList();
        _phase     = Phase.Positioning;
        _targetsSet = false;
    }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        if (!_targetsSet)
            ComputeFlankTargets(playerPos);

        switch (_phase)
        {
            case Phase.Positioning:
                (_lead       as IMovable)?.MoveTo(playerPos);
                (_flankers[0] as IMovable)?.MoveTo(_flankTargetA);
                (_flankers[1] as IMovable)?.MoveTo(_flankTargetB);

                if (FlankersInPosition())
                    _phase = Phase.Rushing;
                break;

            case Phase.Rushing:
                foreach (var agent in _agents)
                    (agent as IMovable)?.MoveTo(playerPos);
                break;
        }
    }

    private void ComputeFlankTargets(Vector3 playerPos)
    {
        _targetsSet = true;

        Vector3 toPlayer = Vector3.ProjectOnPlane(playerPos - _lead.transform.position, Vector3.up);
        if (toPlayer.sqrMagnitude < 0.01f) toPlayer = _lead.transform.forward;
        toPlayer.Normalize();

        Vector3 dirA = Quaternion.Euler(0f,  FlankAngle, 0f) * toPlayer;
        Vector3 dirB = Quaternion.Euler(0f, -FlankAngle, 0f) * toPlayer;

        _flankTargetA = SampleNavMesh(playerPos + dirA * FlankRadius);
        _flankTargetB = SampleNavMesh(playerPos + dirB * FlankRadius);
    }

    private Vector3 SampleNavMesh(Vector3 candidate)
    {
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            return hit.position;
        return candidate;
    }

    private bool FlankersInPosition()
    {
        foreach (var f in _flankers)
            if (f is IMovable m && !m.HasReached) return false;
        return true;
    }

    public override void End()
    {
        foreach (var agent in _agents)
            (agent as IMovable)?.Stop();
    }
}
