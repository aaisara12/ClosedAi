#nullable enable

using DG.Tweening;
using UnityEngine;

/// <summary>
/// Drives the <c>Custom/Explosion</c> shader on a sphere mesh.
/// Call <see cref="Play"/> to start the animation. The sphere should be
/// scaled externally (e.g. via DOTween or AnimationCurve) to represent
/// the blast radius expanding — this script only owns the shader timing.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ExplosionEffect : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float duration = 1.2f;

    [Header("Scale")]
    [SerializeField] private float maxScale      = 3f;
    [SerializeField] private float scaleDuration = 0.4f;
    [SerializeField] private Ease  scaleEase     = Ease.OutExpo;

    [Header("References")]
    [Tooltip("Leave empty to use the Renderer's shared material instance.")]
    [SerializeField] private Renderer? meshRenderer;

    // Shader property IDs — cached to avoid per-frame string lookups
    private static readonly int AgePropId     = Shader.PropertyToID("_Age");
    private static readonly int DurationPropId = Shader.PropertyToID("_Duration");

    private Material? _material;
    private float     _age;
    private bool      _playing;
    private Tweener?  _scaleTween;

    // ─── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        meshRenderer ??= GetComponent<Renderer>();

        // Create a per-instance material so multiple explosions don't share state
        _material = meshRenderer.material;
        _material.SetFloat(DurationPropId, duration);
        _material.SetFloat(AgePropId, 0f);
    }

    private void Update()
    {
        if (!_playing || _material == null)
            return;

        _age += Time.deltaTime;
        _material.SetFloat(AgePropId, _age);

        if (_age >= duration)
            OnComplete();
    }

    // ─── Public API ────────────────────────────────────────────────────────

    /// <summary>Starts (or restarts) the explosion animation from the beginning.</summary>
    public void Play()
    {
        if (_material == null)
            return;

        _age     = 0f;
        _playing = true;

        _material.SetFloat(DurationPropId, duration);
        _material.SetFloat(AgePropId, 0f);

        gameObject.SetActive(true);

        // Scale from zero → maxScale over scaleDuration
        _scaleTween?.Kill();
        transform.localScale = Vector3.zero;
        _scaleTween = transform
            .DOScale(maxScale, scaleDuration)
            .SetEase(scaleEase);
    }

    /// <summary>Stops the animation and resets the shader age to zero.</summary>
    public void Stop()
    {
        _playing = false;
        _age     = 0f;
        _material?.SetFloat(AgePropId, 0f);

        _scaleTween?.Kill();
        transform.localScale = Vector3.zero;
    }

    // ─── Private ───────────────────────────────────────────────────────────

    private void OnComplete()
    {
        _playing = false;
        _scaleTween?.Kill();
        gameObject.SetActive(false);
    }
}
