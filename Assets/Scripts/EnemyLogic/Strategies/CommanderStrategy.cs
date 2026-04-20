
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.AI;

public class CommanderStrategy : Strategy
{
    public override int Priority => 15;
    public override StrategyRequirement[] Requirements => new[]
    {
        new StrategyRequirement { Type = EnemyType.Commander, Count = 1 },
    };

    private const float PositioningRadius = 10f;
    private const int   PositionTries    = 6;

    private CommanderAgent _commanderAgent;
    private NavMeshAgent _commanderNav;
    private SignalManager _commanderSignal;

    public override void OnStart()
    {
        Debug.Log("Starting commander strategy");

        _commanderAgent = _agents[0] as CommanderAgent;
        _commanderNav = _commanderAgent.GetComponent<NavMeshAgent>();
        _commanderSignal = _commanderAgent.GetComponentInChildren<SignalManager>();
        _commanderAgent.GetComponentInChildren<SignalStatus>()?.SetIcon(SignalIcon.Diamond);

        Assert.IsTrue(_commanderAgent != null);
        Assert.IsTrue(_commanderNav != null);
    }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        // run away from player (try going somewhere w/o LOS)
        if (HasLOS(_commanderAgent.transform.position, playerPos) && 
            HasLOS(_commanderNav.destination, playerPos))
        {
            _commanderAgent.MoveTo(FindHiddenPosition(_commanderAgent.transform.position, _commanderAgent.transform.position));
        }

        if (_commanderAgent.canGiveShield())
        {
            // find candidate to give shield to
            var shieldTargets = _commanderSignal.GetAllInGraph();
            var shieldTarget = shieldTargets[Random.Range(0, shieldTargets.Count())]
                .GetComponent<EnemyAgent>();
            if (shieldTarget != null) _commanderAgent.GiveShield(shieldTarget);
        }
    }

    private Vector3 FindHiddenPosition(Vector3 pos, Vector3 fallback)
    {
        for (int i = 0; i < PositionTries; i++)
        {
            float angle     = Random.Range(0f, 360f);
            Vector3 offset   = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * PositioningRadius;
            Vector3 candidate = pos + offset;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)
                && !HasLOS(hit.position, pos))
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
        _commanderAgent.GetComponentInChildren<SignalStatus>()?.ResetIcon();
        (_commanderAgent as IMovable)?.Stop();
    }
}
