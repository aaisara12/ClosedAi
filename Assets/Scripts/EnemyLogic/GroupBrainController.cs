using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(EnemyAgent))]
public class GroupBrainController : MonoBehaviour
{
    private SignalManager _signalManager;
    private EnemyAgent _agent;
    private bool _rebuildPending;

    private void Awake()
    {
        _signalManager = GetComponentInChildren<SignalManager>();
        Assert.IsTrue(_signalManager != null);
        _agent = GetComponent<EnemyAgent>();
    }

    private void OnEnable() => _signalManager.OnConnectionChanged += HandleConnectionChanged;
    private void OnDisable() => _signalManager.OnConnectionChanged -= HandleConnectionChanged;
    private void Start() => ScheduleRebuild();

    private void HandleConnectionChanged(Signal _, bool __) => ScheduleRebuild();

    private void ScheduleRebuild()
    {
        if (_rebuildPending) return;
        _rebuildPending = true;
        StartCoroutine(DeferredRebuild());
    }

    private IEnumerator DeferredRebuild()
    {
        yield return null;
        _rebuildPending = false;
        RebuildGroup();
    }

    private void RebuildGroup()
    {
        var allManagers = _signalManager.GetAllInGraph();
        allManagers.Add(_signalManager);

        // Lowest instance ID in the group leads the rebuild so it only runs once
        SignalManager leader = allManagers[0];
        foreach (var m in allManagers)
            if (m.GetInstanceID() < leader.GetInstanceID()) leader = m;
        if (leader != _signalManager) return;

        var agents = allManagers
            .Select(m => m.GetComponentInParent<EnemyAgent>())
            .Where(a => a != null)
            .ToList();

        foreach (var old in agents.Select(a => a.Brain).Where(b => b != null).Distinct().ToList())
            old.Dissolve();

        var brainGO = new GameObject("GroupBrain");
        var brain = brainGO.AddComponent<GroupBrain>();
        brain.AddMembers(agents);
    }
}
