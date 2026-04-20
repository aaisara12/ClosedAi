using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RushdownStrategy : Strategy
{
    public override int Priority => 9;
    public override StrategyRequirement[] Requirements => new[]
    {
        new StrategyRequirement { Type = EnemyType.Melee, Count = 4 },
    };

    private const float SpeedMultiplier = 2f;

    private readonly Dictionary<EnemyAgent, float> _originalSpeeds = new();

    public override void OnStart()
    {
        foreach (var a in _agents)
            a.GetComponentInChildren<SignalStatus>()?.SetIcon(SignalIcon.Triangle);
        foreach (var agent in _agents)
        {
            Debug.Log("Starting Rushdown strategy");

            var nav = agent.GetComponent<NavMeshAgent>();
            if (nav == null) continue;
            _originalSpeeds[agent] = nav.speed;
            nav.speed *= SpeedMultiplier;
        }
    }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        foreach (var agent in _agents)
            (agent as IMovable)?.MoveTo(playerPos);
    }

    public override void End()
    {
        foreach (var a in _agents)
            a.GetComponentInChildren<SignalStatus>()?.ResetIcon();
        foreach (var agent in _agents)
        {
            (agent as IMovable)?.Stop();
            var nav = agent.GetComponent<NavMeshAgent>();
            if (nav != null && _originalSpeeds.TryGetValue(agent, out float original))
                nav.speed = original;
        }
    }
}
