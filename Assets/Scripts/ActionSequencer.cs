#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct ActionSequenceStep
{
    [Tooltip("Seconds to wait after the previous step fires (or after Play is called for the first step) before invoking this step's event.")]
    [Min(0f)]
    public float delay;

    [Tooltip("The event(s) to invoke for this step.")]
    public UnityEvent action;
}

/// <summary>
/// Plays a list of <see cref="ActionSequenceStep"/> entries in order.
/// Each step waits its configured delay then fires its <see cref="UnityEvent"/>.
/// </summary>
public class ActionSequencer : MonoBehaviour
{
    [SerializeField] private List<ActionSequenceStep> steps = new();

    [Tooltip("If true, the sequence will start automatically when the GameObject starts.")]
    [SerializeField] private bool playOnStart = false;

    [SerializeField] private UnityEvent onSequenceComplete = new();

    private Coroutine? _activeSequence;

    // ─── Unity Messages ────────────────────────────────────────────────────────

    private void Start()
    {
        if (playOnStart)
            PlaySequence();
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    /// <summary>Starts the sequence from the beginning. If already running, restarts it.</summary>
    public void PlaySequence()
    {
        if (_activeSequence != null)
            StopCoroutine(_activeSequence);

        _activeSequence = StartCoroutine(SequenceCoroutine());
    }

    /// <summary>Stops the sequence immediately without invoking remaining steps.</summary>
    public void StopSequence()
    {
        if (_activeSequence != null)
        {
            StopCoroutine(_activeSequence);
            _activeSequence = null;
        }
    }

    // ─── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator SequenceCoroutine()
    {
        foreach (var step in steps)
        {
            if (step.delay > 0f)
                yield return new WaitForSeconds(step.delay);

            step.action?.Invoke();
        }

        _activeSequence = null;
        onSequenceComplete?.Invoke();
    }
}

