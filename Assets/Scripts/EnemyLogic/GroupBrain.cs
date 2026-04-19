using System.Collections.Generic;
using UnityEngine;

public class GroupBrain : MonoBehaviour
{
    [Header("Confirmation")]
    [SerializeField] private float _confirmDuration = 1.5f;
    [SerializeField] private float _lossDuration = 3f;
    // Should exceed PlayerDetector._checkInterval to absorb scan gaps
    [SerializeField] private float _scanTimeout = 1.5f;

    private enum State { Inactive, Confirming, Executing }
    private State _state = State.Inactive;

    private readonly List<EnemyAgent> _members = new();
    private List<Strategy> _strategies = new();
    private float _lastReportTime = float.NegativeInfinity;
    private float _confirmAccumulated;
    private float _lossAccumulated;

    public Vector3 LastKnownPlayerPosition { get; private set; }
    public bool IsConfirming => _state == State.Confirming;
    public bool IsExecuting  => _state == State.Executing;

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

    public void RemoveMember(EnemyAgent agent)
    {
        if (!_members.Remove(agent)) return;
        agent.Brain = null;
        Reassign();
    }

    // Called by PlayerDetector on any member that has LOS on the player
    public void ReportPlayerSpotted(Vector3 position)
    {
        LastKnownPlayerPosition = position;
        _lastReportTime = Time.time;

        if (_state == State.Inactive)
        {
            _state = State.Confirming;
            _confirmAccumulated = 0f;
        }
    }

    private void Update()
    {
        bool hasLOS = Time.time - _lastReportTime < _scanTimeout;

        switch (_state)
        {
            case State.Confirming:
                // Accumulate only while LOS is held; pause (not reset) when cover is broken
                if (hasLOS)
                {
                    _confirmAccumulated += Time.deltaTime;
                    if (_confirmAccumulated >= _confirmDuration)
                        EnterExecuting();
                }
                break;

            case State.Executing:
                if (hasLOS)
                {
                    _lossAccumulated = 0f;
                }
                else
                {
                    _lossAccumulated += Time.deltaTime;
                    if (_lossAccumulated >= _lossDuration)
                        EnterInactive();
                }

                foreach (var s in _strategies)
                    s.Tick(PlayerController.Instance.transform.position, true);
                break;
        }
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

    private void EnterExecuting()
    {
        _state = State.Executing;
        _lossAccumulated = 0f;
        foreach (var m in _members)
            m.OnPlayerSpotted(LastKnownPlayerPosition);
    }

    private void EnterInactive()
    {
        _state = State.Inactive;
        _confirmAccumulated = 0f;
    }

    private void Reassign()
    {
        foreach (var s in _strategies) s.End();
        _strategies = StrategyPicker.Assign(new List<EnemyAgent>(_members));
    }
}
