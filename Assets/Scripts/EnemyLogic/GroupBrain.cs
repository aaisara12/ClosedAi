using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    private readonly Dictionary<EnemyAgent, Coroutine> _patrolCoroutines = new();
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
        if (_state != State.Executing) StartPatrol();
    }

    public void AddMember(EnemyAgent agent)
    {
        if (_members.Contains(agent)) return;
        _members.Add(agent);
        agent.Brain = this;
        Reassign();
        if (_state != State.Executing) StartPatrol();
    }

    public void RemoveMember(EnemyAgent agent)
    {
        if (!_members.Remove(agent)) return;
        agent.Brain = null;
        StopAgentPatrol(agent);
        Reassign();
    }

    // Called by PlayerDetector on any member that has LOS on the player
    public void ReportPlayerSpotted(Vector3 position)
    {
        LastKnownPlayerPosition = position;
        _lastReportTime = Time.time;

        if (_state == State.Inactive)
        {
            AudioSystem.Play(AudioSystem.Sound.EnemySuspicious);
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
        StopPatrol();
        foreach (var s in _strategies) s.End();
        foreach (var m in new List<EnemyAgent>(_members))
            m.Brain = null;
        _members.Clear();
        _strategies.Clear();
        Destroy(gameObject);
    }

    private void EnterExecuting()
    {
        AudioSystem.Play(AudioSystem.Sound.EnemyAggressive);
        _state = State.Executing;
        _lossAccumulated = 0f;
        StopPatrol();
        foreach (var m in _members)
            m.OnPlayerSpotted(LastKnownPlayerPosition);
    }

    private void EnterInactive()
    {
        _state = State.Inactive;
        _confirmAccumulated = 0f;
        StartPatrol();
    }

    private void StartPatrol()
    {
        foreach (var agent in _members)
        {
            if (agent is not IMovable) continue;
            if (_patrolCoroutines.ContainsKey(agent)) continue;
            _patrolCoroutines[agent] = StartCoroutine(PatrolAgent(agent));
        }
    }

    private void StopPatrol()
    {
        foreach (var kvp in _patrolCoroutines)
        {
            if (kvp.Value != null) StopCoroutine(kvp.Value);
            if (kvp.Key is IMovable movable) movable.Stop();
        }
        _patrolCoroutines.Clear();
    }

    private void StopAgentPatrol(EnemyAgent agent)
    {
        if (!_patrolCoroutines.TryGetValue(agent, out Coroutine coroutine)) return;
        if (coroutine != null) StopCoroutine(coroutine);
        if (agent is IMovable movable) movable.Stop();
        _patrolCoroutines.Remove(agent);
    }

    private IEnumerator PatrolAgent(EnemyAgent agent)
    {
        IMovable movable = agent as IMovable;

        while (agent != null)
        {
            while (agent != null && agent.IsIsolated)
                yield return null;
            if (agent == null) yield break;

            Vector3 destination = SamplePatrolPoint(agent.PatrolOrigin, agent.PatrolRadius);
            movable.MoveTo(destination);
            yield return null;

            while (agent != null && !movable.HasReached && !agent.IsIsolated)
                yield return null;

            if (agent == null) yield break;
            if (agent.IsIsolated) continue;
            movable.Stop();

            float scanDir = Random.value > 0.5f ? 1f : -1f;
            float scanElapsed = 0f;
            while (agent != null && scanElapsed < agent.PatrolScanDuration && !agent.IsIsolated)
            {
                agent.transform.Rotate(Vector3.up, scanDir * agent.PatrolScanSpeed * Time.deltaTime);
                scanElapsed += Time.deltaTime;
                yield return null;
            }

            if (agent == null) yield break;
            if (agent.IsIsolated) continue;

            float pauseElapsed = 0f;
            while (agent != null && pauseElapsed < agent.PatrolPauseDuration && !agent.IsIsolated)
            {
                pauseElapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private static Vector3 SamplePatrolPoint(Vector3 origin, float radius)
    {
        const float snapRadius = 2f;
        const float maxHeightDiff = 1.5f;

        Vector2 circle = Random.insideUnitCircle * radius;
        Vector3 candidate = origin + new Vector3(circle.x, 0f, circle.y);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, snapRadius, NavMesh.AllAreas)
            && Mathf.Abs(hit.position.y - origin.y) <= maxHeightDiff)
            return hit.position;
        return origin;
    }

    private void Reassign()
    {
        foreach (var s in _strategies) s.End();
        _strategies = StrategyPicker.Assign(new List<EnemyAgent>(_members));
    }
}
