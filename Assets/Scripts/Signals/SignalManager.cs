using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;
using UnityEngine.Serialization;

public class SignalManager : MonoBehaviour
{
    // ── Inspector Config ──────────────────────────────────────────────────────

    [Header("Connection Settings")]
    [SerializeField] private int maxConnections = 4;
    [SerializeField] private float connectionRadius = 15f;
    [SerializeField] private float reconnectDelay = 3f;     // seconds before re-trying a broken link
    [SerializeField] private float disabledDuration = 8f;   // seconds disabled when fully isolated

    [Header("Signal Prefab")]
    [SerializeField] private Signal signalPrefab;

    public Signal ActiveSignal;
    
    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fires when any connection is formed or broken.
    /// Parameters: (Signal signal, bool formed)  — formed=true → new link, false → broken
    /// </summary>
    public event Action<Signal, bool> OnConnectionChanged;

    /// <summary>Fires when every connection has been severed.</summary>
    public event Action OnFullyIsolated;

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly List<Signal> _connections = new();
    private readonly HashSet<SignalManager> _pendingReconnect = new();
    private bool _isDisabled = false;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        StartCoroutine(InitialConnect());
    }

    private void OnDestroy()
    {
        var toBreak = new List<Signal>(_connections);
        foreach (Signal signal in toBreak)
        {
            SignalManager other = signal.GetOther(this);
            other?.OnSignalBroken(signal);
            if (signal != null) Destroy(signal.gameObject);
        }
    }

    // Small delay so all enemies have initialised before we try to link up.
    private IEnumerator InitialConnect()
    {
        yield return new WaitForEndOfFrame();
        TryScanAndConnect();
    }

    // ── Public Interface ──────────────────────────────────────────────────────

    /// <summary>
    /// Attempt to connect to <paramref name="other"/>.
    /// Creates a Signal object if both sides accept.
    /// </summary>
    public bool TryConnectTo(SignalManager other)
    {
        if (!CanAcceptConnection()) return false;
        if (other == this) return false;
        if (IsConnectedTo(other)) return false;

        // Ask the other side whether it can accept
        if (!other.ReceiveConnectionRequest(this)) return false;

        // Both sides agree — instantiate the Signal
        Signal signal = Instantiate(signalPrefab);
        signal.Initialise(this, other);

        _connections.Add(signal);
        other._connections.Add(signal);

        OnConnectionChanged?.Invoke(signal, true);
        other.OnConnectionChanged?.Invoke(signal, true);

        ActiveSignal = signal;
        other.ActiveSignal = signal;
        return true;
    }

    /// <summary>
    /// Called by a remote SignalManager asking to connect.
    /// Returns false if this manager is at capacity, disabled, or already connected.
    /// </summary>
    public bool ReceiveConnectionRequest(SignalManager requester)
    {
        if (_isDisabled) return false;
        if (!CanAcceptConnection()) return false;
        if (IsConnectedTo(requester)) return false;
        return true;
    }

    public void DisconnectFromAll()
    {
        foreach(Signal s in new List<Signal>(_connections))
        {
            s.Break();
        }
    }

    /// <summary>
    /// Called by a Signal when it is broken (e.g. destroyed by the player).
    /// </summary>
    public void OnSignalBroken(Signal signal)
    {
        if (!_connections.Remove(signal)) return;

        SignalManager other = signal.GetOther(this);
        OnConnectionChanged?.Invoke(signal, false);

        // Schedule a reconnect attempt toward the broken partner
        if (other != null)
            StartCoroutine(ScheduleReconnect(other));

        // Check for full isolation
        if (_connections.Count == 0)
        {
            OnFullyIsolated?.Invoke();
            StartCoroutine(DisableTemporarily());
        }

        ActiveSignal = null;
    }

    /// <summary>Returns all SignalManagers reachable in the signal graph (BFS).</summary>
    public List<SignalManager> GetAllInGraph()
    {
        var visited = new HashSet<SignalManager> { this };
        var queue = new Queue<SignalManager>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            SignalManager current = queue.Dequeue();
            foreach (Signal s in current._connections)
            {
                SignalManager neighbour = s.GetOther(current);
                if (neighbour != null && visited.Add(neighbour))
                    queue.Enqueue(neighbour);
            }
        }

        visited.Remove(this); // caller usually wants others, not self
        return new List<SignalManager>(visited);
    }

    /// <summary>Returns only the directly connected SignalManagers.</summary>
    public List<SignalManager> GetDirectNeighbours()
    {
        var neighbours = new List<SignalManager>(_connections.Count);
        foreach (Signal s in _connections)
        {
            SignalManager other = s.GetOther(this);
            if (other != null) neighbours.Add(other);
        }
        return neighbours;
    }

    /// <summary>Active signals this manager holds.</summary>
    public IReadOnlyList<Signal> Connections => _connections;

    public void BreakActiveSignal()
    {
        ActiveSignal?.Break();
    }
    
    // ── Private Helpers ───────────────────────────────────────────────────────

    private bool CanAcceptConnection() =>
        !_isDisabled && _connections.Count < maxConnections;

    private bool IsConnectedTo(SignalManager other)
    {
        foreach (Signal s in _connections)
            if (s.GetOther(this) == other) return true;
        return false;
    }

    /// <summary>
    /// Scans the area for nearby SignalManagers and tries to fill connection slots.
    /// </summary>
    private void TryScanAndConnect()
    {
        if (_isDisabled) return;
        if (_connections.Count >= maxConnections) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, connectionRadius);

        List<SignalManager> candidates = hits
            .Select(hit => hit.GetComponent<SignalManager>())
            .Where(sm => sm != null && sm != this)
            .ToList();


        var alreadyConnected = GetAllInGraph();
        List<SignalManager> prioCandidates = candidates
            .Where(c => !alreadyConnected.Contains(c))
            .ToList();
        foreach(var c in prioCandidates)
        {
            TryConnectTo(c);
            if (_connections.Count >= maxConnections) return;
        }

        List<SignalManager> otherCandidates = candidates
            .Where(c => alreadyConnected.Contains(c))
            .ToList();
        foreach(var c in otherCandidates)
        {
            TryConnectTo(c);
            if (_connections.Count >= maxConnections) return;
        }
    }

    private IEnumerator ScheduleReconnect(SignalManager target)
    {
        if (!_pendingReconnect.Add(target)) yield break; // already waiting

        yield return new WaitForSeconds(reconnectDelay);
        _pendingReconnect.Remove(target);

        if (target == null) yield break;
        if (!_isDisabled && !IsConnectedTo(target))
            TryConnectTo(target);
    }

    private IEnumerator DisableTemporarily()
    {
        _isDisabled = true;
        yield return new WaitForSeconds(disabledDuration);
        _isDisabled = false;

        // After coming back online, scan for new connections
        TryScanAndConnect();
    }
}
