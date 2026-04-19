#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct FadeInOutSequenceStep
{
    [Tooltip("The animator to fade in and out for this step.")]
    public FadeInOutAnimator? animator;

    [Tooltip("Seconds to wait after the previous step's fade-out completes (or sequence start for the first step) before fading in.")]
    [Min(0f)]
    public float delayBeforeFadeIn;

    [Tooltip("Seconds to hold at full opacity after fade-in completes before fading out.")]
    [Min(0f)]
    public float holdDuration;
}

/// <summary>
/// Sequences a series of <see cref="FadeInOutAnimator"/> fade-in/out pairs with configurable
/// per-step delays and hold durations. Steps are played strictly one after another with no overlap.
/// </summary>
public class FadeInOutSequencer : MonoBehaviour
{
    [SerializeField] private List<FadeInOutSequenceStep> steps = new();

    [SerializeField] private UnityEvent onSequenceComplete;
    
    private Coroutine? _activeSequence;
    private int _activeStepIndex = -1;

    // ─── Public API ────────────────────────────────────────────────────────────

    public void PlaySequence()
    {
        if (_activeSequence != null)
            StopCoroutine(_activeSequence);

        _activeSequence = StartCoroutine(CleanupAndPlayCoroutine());
    }

    // ─── Coroutines ────────────────────────────────────────────────────────────

    /// <summary>
    /// If a sequence was interrupted mid-step, fades out the active animator before
    /// starting the sequence fresh from the beginning.
    /// </summary>
    private IEnumerator CleanupAndPlayCoroutine()
    {
        if (_activeStepIndex >= 0 && _activeStepIndex < steps.Count)
        {
            var interrupted = steps[_activeStepIndex];
            if (interrupted.animator != null)
            {
                yield return StartCoroutine(interrupted.animator.FadeOutLoadingScreenCoroutine());
            }
        }

        _activeStepIndex = -1;
        yield return StartCoroutine(PlaySequenceCoroutine());
    }

    private IEnumerator PlaySequenceCoroutine()
    {
        for (int i = 0; i < steps.Count; i++)
        {
            _activeStepIndex = i;
            var step = steps[i];

            if (step.animator == null)
            {
                Debug.LogWarning($"LoadingScreenSequencer: Step {i} has no animator assigned — skipping.");
                continue;
            }

            // Wait after the previous step's fade-out (or sequence start for step 0).
            if (step.delayBeforeFadeIn > 0f)
                yield return new WaitForSeconds(step.delayBeforeFadeIn);

            // Fade in and wait for it to complete.
            yield return StartCoroutine(step.animator.FadeInLoadingScreenCoroutine());

            // Hold at full opacity.
            if (step.holdDuration > 0f)
                yield return new WaitForSeconds(step.holdDuration);

            // Fade out and wait for it to complete before moving to the next step.
            yield return StartCoroutine(step.animator.FadeOutLoadingScreenCoroutine());
        }

        _activeStepIndex = -1;
        _activeSequence = null;
        onSequenceComplete.Invoke();
    }
}

