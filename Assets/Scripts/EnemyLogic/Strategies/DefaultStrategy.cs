using System;
using System.Collections.Generic;
using UnityEngine;

public class DefaultStrategy : Strategy
{
    public override int Priority => 0;
    public override StrategyRequirement[] Requirements => Array.Empty<StrategyRequirement>();

    public void AssignAll(List<EnemyAgent> agents)
    {
        _agents = agents;
        OnStart();
    }

    public override void OnStart()
    {
        Debug.Log("Starting default strategy");
        foreach (var agent in _agents)
            agent.GetComponentInChildren<SignalStatus>()?.SetIcon(SignalIcon.RedCircle);
    }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        if (!playerSpotted) return;
        foreach (var agent in _agents)
            if (agent is IMovable m) m.MoveTo(playerPos);
    }

    public override void End()
    {
        foreach (var agent in _agents)
            agent.GetComponentInChildren<SignalStatus>()?.ResetIcon();
        foreach (var agent in _agents)
            if (agent is IMovable m) m.Stop();
    }
}
