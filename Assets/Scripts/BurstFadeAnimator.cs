#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// Plays a fast fade-in burst followed by a slow fade-out on a <see cref="CanvasGroup"/>.
/// Calling <see cref="Play"/> while already playing will kill and restart from the beginning.
/// </summary>
public class BurstFadeAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup? canvasGroup;

    [Header("Burst Fade Settings")]
    [SerializeField, Min(0f)] private float burstFadeInDuration = 0.1f;
    [SerializeField, Min(0f)] private float holdDuration = 0.1f;
    [SerializeField, Min(0f)] private float slowFadeOutDuration = 0.8f;

    [Header("Easing")]
    [SerializeField] private Ease fadeInEase = Ease.OutExpo;
    [SerializeField] private Ease fadeOutEase = Ease.InSine;

    [Header("Events")]
    [SerializeField] private UnityEvent onComplete = new();

    [Header("Auto Play")]
    [SerializeField] private bool playOnStart = false;

    private Sequence? _sequence;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            Debug.LogError($"[BurstFadeAnimator] CanvasGroup is not assigned on '{name}'!");
            return;
        }

        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    private void OnDisable()
    {
        KillSequence();
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Plays the burst fade sequence. If already playing, restarts from the beginning.
    /// </summary>
    public void Play()
    {
        if (canvasGroup == null)
        {
            Debug.LogError($"[BurstFadeAnimator] CanvasGroup is null on '{name}' — cannot play.");
            return;
        }

        StartBurstFade();
    }

    /// <summary>
    /// Coroutine variant of <see cref="Play"/>. Yields until the sequence finishes.
    /// </summary>
    public IEnumerator PlayCoroutine()
    {
        if (canvasGroup == null)
        {
            Debug.LogError($"[BurstFadeAnimator] CanvasGroup is null on '{name}' — cannot play.");
            yield break;
        }

        StartBurstFade();
        yield return new WaitWhile(() => _sequence != null && _sequence.IsActive() && _sequence.IsPlaying());
    }

    // ─── Private ───────────────────────────────────────────────────────────────

    private void StartBurstFade()
    {
        KillSequence();

        var cg = canvasGroup!;
        cg.alpha = 0f;

        _sequence = DOTween.Sequence()
            .Append(DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, burstFadeInDuration).SetEase(fadeInEase))
            .AppendInterval(holdDuration)
            .Append(DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, slowFadeOutDuration).SetEase(fadeOutEase))
            .OnComplete(() =>
            {
                _sequence = null;
                onComplete.Invoke();
            });
    }

    private void KillSequence()
    {
        if (_sequence == null) return;
        _sequence.Kill();
        _sequence = null;
    }
}

