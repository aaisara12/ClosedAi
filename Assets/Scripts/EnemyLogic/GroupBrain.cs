using System.Collections.Generic;
using UnityEngine;

public class GroupBrain : MonoBehaviour
{
    [SerializeField] private float _spotTimeout = 5f;

    private readonly List<EnemyAgent> _members = new();
    private List<Strategy> _strategies = new();
    private float _lastSpotTime;

    public Vector3 LastKnownPlayerPosition { get; private set; }
    public bool PlayerSpotted { get; private set; }

    public void AddMembers(List<EnemyAgent> agents)
    {
        foreach (var a in agents)
        {
            if (_members.Contains(a)) continue;
            _members.Add(a);
            a.Brain = this;
        }
        Reassign();
    }

    public void AddMember(EnemyAgent agent)
    {
        if (_members.Contains(agent)) return;
        _members.Add(agent);
        agent.Brain = this;
        Reassign();
    }

    // Called by the connection system when this group loses a member
    public void RemoveMember(EnemyAgent agent)
    {
        if (!_members.Remove(agent)) return;
        agent.Brain = null;
        Reassign();
    }

    // Any member that spots the player calls this; all members are informed
    public void ReportPlayerSpotted(Vector3 position)
    {
        LastKnownPlayerPosition = position;
        PlayerSpotted = true;
        _lastSpotTime = Time.time;
        foreach (var m in _members)
            m.OnPlayerSpotted(position);
    }

    private void Update()
    {
        if (PlayerSpotted && Time.time - _lastSpotTime > _spotTimeout)
            PlayerSpotted = false;

        foreach (var s in _strategies)
            s.Tick(LastKnownPlayerPosition, PlayerSpotted);
    }

    public void Dissolve()
    {
        foreach (var s in _strategies) s.End();
        foreach (var m in new List<EnemyAgent>(_members))
            m.Brain = null;
        _members.Clear();
        _strategies.Clear();
        Destroy(gameObject);
    }

    private void Reassign()
    {
        foreach (var s in _strategies) s.End();
        _strategies = StrategyPicker.Assign(new List<EnemyAgent>(_members));
    }
}
