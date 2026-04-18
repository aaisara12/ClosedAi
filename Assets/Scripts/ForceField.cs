using System.Collections;
using UnityEngine;

/// <summary>
/// Drives per-instance force-field shader properties via MaterialPropertyBlock,
/// avoiding the creation of extra material instances.
/// Attach to any GameObject that has a Renderer using the Custom/ForceField shader.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ForceField : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector-exposed properties (mirror shader properties)
    // -------------------------------------------------------------------------
    [Header("Layer 1")]
    public Texture2D layer1Tex;
    public float layer1Tiling                = 1.0f;
    public float layer1ScrollX               = 0.1f;
    public float layer1ScrollY               = 0.2f;
    public Color layer1Color                 = new Color(0.2f, 0.6f, 1.0f, 1.0f);
    public float layer1FadeMin               = 0.3f;
    public float layer1FadeMax               = 1.0f;
    public float layer1FadeDuration          = 2.0f;
    public float layer1FadeOffset            = 0.0f;
    public int   layer1FadeMode              = 0;   // 0 = Sin, 1 = Lightning
    public float layer1LightningFlashWidth   = 0.1f;

    [Header("Layer 2")]
    public Texture2D layer2Tex;
    public float layer2Tiling                = 1.5f;
    public float layer2ScrollX               = -0.15f;
    public float layer2ScrollY               = 0.1f;
    public Color layer2Color                 = new Color(0.1f, 0.4f, 0.9f, 1.0f);
    public float layer2FadeMin               = 0.3f;
    public float layer2FadeMax               = 1.0f;
    public float layer2FadeDuration          = 2.0f;
    public float layer2FadeOffset            = 0.333f;
    public int   layer2FadeMode              = 0;
    public float layer2LightningFlashWidth   = 0.1f;

    [Header("Layer 3")]
    public Texture2D layer3Tex;
    public float layer3Tiling                = 2.5f;
    public float layer3ScrollX               = 0.05f;
    public float layer3ScrollY               = -0.25f;
    public Color layer3Color                 = new Color(0.5f, 0.8f, 1.0f, 1.0f);
    public float layer3FadeMin               = 0.3f;
    public float layer3FadeMax               = 1.0f;
    public float layer3FadeDuration          = 2.0f;
    public float layer3FadeOffset            = 0.667f;
    public int   layer3FadeMode              = 0;
    public float layer3LightningFlashWidth   = 0.1f;

    [Header("Fresnel")]
    public float fresnelPower       = 2.0f;
    public Color fresnelColor       = new Color(0.3f, 0.7f, 1.0f, 1.0f);

    [Header("Hit Pulse")]
    [Tooltip("How high FresnelPower spikes on a hit.")]
    public float pulseFresnelPeak   = 8.0f;
    [Tooltip("How long (seconds) the pulse takes to fade back to the base value.")]
    public float pulseDuration      = 0.4f;

    // -------------------------------------------------------------------------
    // Private
    // -------------------------------------------------------------------------
    private Renderer          _renderer;
    private MaterialPropertyBlock _mpb;
    private Coroutine         _pulseRoutine;

    // Cached shader property IDs
    private static readonly int ID_Layer1Tex          = Shader.PropertyToID("_Layer1Tex");
    private static readonly int ID_Layer1Tiling       = Shader.PropertyToID("_Layer1Tiling");
    private static readonly int ID_Layer1ScrollX      = Shader.PropertyToID("_Layer1ScrollX");
    private static readonly int ID_Layer1ScrollY      = Shader.PropertyToID("_Layer1ScrollY");
    private static readonly int ID_Layer1Color        = Shader.PropertyToID("_Layer1Color");
    private static readonly int ID_Layer1FadeMin      = Shader.PropertyToID("_Layer1FadeMin");
    private static readonly int ID_Layer1FadeMax      = Shader.PropertyToID("_Layer1FadeMax");
    private static readonly int ID_Layer1FadeDuration = Shader.PropertyToID("_Layer1FadeDuration");
    private static readonly int ID_Layer1FadeOffset   = Shader.PropertyToID("_Layer1FadeOffset");
    private static readonly int ID_Layer1FadeMode     = Shader.PropertyToID("_Layer1FadeMode");
    private static readonly int ID_Layer1LightningFlashWidth   = Shader.PropertyToID("_Layer1LightningFlashWidth");

    private static readonly int ID_Layer2Tex          = Shader.PropertyToID("_Layer2Tex");
    private static readonly int ID_Layer2Tiling       = Shader.PropertyToID("_Layer2Tiling");
    private static readonly int ID_Layer2ScrollX      = Shader.PropertyToID("_Layer2ScrollX");
    private static readonly int ID_Layer2ScrollY      = Shader.PropertyToID("_Layer2ScrollY");
    private static readonly int ID_Layer2Color        = Shader.PropertyToID("_Layer2Color");
    private static readonly int ID_Layer2FadeMin      = Shader.PropertyToID("_Layer2FadeMin");
    private static readonly int ID_Layer2FadeMax      = Shader.PropertyToID("_Layer2FadeMax");
    private static readonly int ID_Layer2FadeDuration = Shader.PropertyToID("_Layer2FadeDuration");
    private static readonly int ID_Layer2FadeOffset   = Shader.PropertyToID("_Layer2FadeOffset");
    private static readonly int ID_Layer2FadeMode     = Shader.PropertyToID("_Layer2FadeMode");
    private static readonly int ID_Layer2LightningFlashWidth   = Shader.PropertyToID("_Layer2LightningFlashWidth");

    private static readonly int ID_Layer3Tex          = Shader.PropertyToID("_Layer3Tex");
    private static readonly int ID_Layer3Tiling       = Shader.PropertyToID("_Layer3Tiling");
    private static readonly int ID_Layer3ScrollX      = Shader.PropertyToID("_Layer3ScrollX");
    private static readonly int ID_Layer3ScrollY      = Shader.PropertyToID("_Layer3ScrollY");
    private static readonly int ID_Layer3Color        = Shader.PropertyToID("_Layer3Color");
    private static readonly int ID_Layer3FadeMin      = Shader.PropertyToID("_Layer3FadeMin");
    private static readonly int ID_Layer3FadeMax      = Shader.PropertyToID("_Layer3FadeMax");
    private static readonly int ID_Layer3FadeDuration = Shader.PropertyToID("_Layer3FadeDuration");
    private static readonly int ID_Layer3FadeOffset   = Shader.PropertyToID("_Layer3FadeOffset");
    private static readonly int ID_Layer3FadeMode     = Shader.PropertyToID("_Layer3FadeMode");
    private static readonly int ID_Layer3LightningFlashWidth   = Shader.PropertyToID("_Layer3LightningFlashWidth");

    private static readonly int ID_FresnelPower       = Shader.PropertyToID("_FresnelPower");
    private static readonly int ID_FresnelColor       = Shader.PropertyToID("_FresnelColor");

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();
        ApplyProperties();
    }

    private void OnValidate()
    {
        // Live-update in the editor while tweaking values
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        ApplyProperties();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies all current property values to the renderer's MaterialPropertyBlock.
    /// Call this after changing any public field at runtime.
    /// </summary>
    public void ApplyProperties()
    {
        _renderer.GetPropertyBlock(_mpb);

        _mpb.SetTexture(ID_Layer1Tex,          layer1Tex != null ? layer1Tex : Texture2D.whiteTexture);
        _mpb.SetFloat(ID_Layer1Tiling,         layer1Tiling);
        _mpb.SetFloat(ID_Layer1ScrollX,        layer1ScrollX);
        _mpb.SetFloat(ID_Layer1ScrollY,        layer1ScrollY);
        _mpb.SetColor(ID_Layer1Color,          layer1Color);
        _mpb.SetFloat(ID_Layer1FadeMin,        layer1FadeMin);
        _mpb.SetFloat(ID_Layer1FadeMax,        layer1FadeMax);
        _mpb.SetFloat(ID_Layer1FadeDuration,   layer1FadeDuration);
        _mpb.SetFloat(ID_Layer1FadeOffset,     layer1FadeOffset);
        _mpb.SetInt(ID_Layer1FadeMode,         layer1FadeMode);
        _mpb.SetFloat(ID_Layer1LightningFlashWidth, layer1LightningFlashWidth);

        _mpb.SetTexture(ID_Layer2Tex,          layer2Tex != null ? layer2Tex : Texture2D.whiteTexture);
        _mpb.SetFloat(ID_Layer2Tiling,         layer2Tiling);
        _mpb.SetFloat(ID_Layer2ScrollX,        layer2ScrollX);
        _mpb.SetFloat(ID_Layer2ScrollY,        layer2ScrollY);
        _mpb.SetColor(ID_Layer2Color,          layer2Color);
        _mpb.SetFloat(ID_Layer2FadeMin,        layer2FadeMin);
        _mpb.SetFloat(ID_Layer2FadeMax,        layer2FadeMax);
        _mpb.SetFloat(ID_Layer2FadeDuration,   layer2FadeDuration);
        _mpb.SetFloat(ID_Layer2FadeOffset,     layer2FadeOffset);
        _mpb.SetInt(ID_Layer2FadeMode,         layer2FadeMode);
        _mpb.SetFloat(ID_Layer2LightningFlashWidth, layer2LightningFlashWidth);

        _mpb.SetTexture(ID_Layer3Tex,          layer3Tex != null ? layer3Tex : Texture2D.whiteTexture);
        _mpb.SetFloat(ID_Layer3Tiling,         layer3Tiling);
        _mpb.SetFloat(ID_Layer3ScrollX,        layer3ScrollX);
        _mpb.SetFloat(ID_Layer3ScrollY,        layer3ScrollY);
        _mpb.SetColor(ID_Layer3Color,          layer3Color);
        _mpb.SetFloat(ID_Layer3FadeMin,        layer3FadeMin);
        _mpb.SetFloat(ID_Layer3FadeMax,        layer3FadeMax);
        _mpb.SetFloat(ID_Layer3FadeDuration,   layer3FadeDuration);
        _mpb.SetFloat(ID_Layer3FadeOffset,     layer3FadeOffset);
        _mpb.SetInt(ID_Layer3FadeMode,         layer3FadeMode);
        _mpb.SetFloat(ID_Layer3LightningFlashWidth, layer3LightningFlashWidth);

        _mpb.SetFloat(ID_FresnelPower,         fresnelPower);
        _mpb.SetColor(ID_FresnelColor,         fresnelColor);

        _renderer.SetPropertyBlock(_mpb);
    }

    /// <summary>
    /// Spikes the fresnel rim to <see cref="pulseFresnelPeak"/> then smoothly
    /// fades it back to <see cref="fresnelPower"/> over <see cref="pulseDuration"/> seconds.
    /// Safe to call multiple times — re-triggers the pulse each time.
    /// </summary>
    public void PulseOnHit()
    {
        if (_pulseRoutine != null)
            StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(PulseRoutine());
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------
    private IEnumerator PulseRoutine()
    {
        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;

            // Spike at t=0, decay back to base by t=1
            float currentPower = Mathf.Lerp(pulseFresnelPeak, fresnelPower, t);

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ID_FresnelPower, currentPower);
            _renderer.SetPropertyBlock(_mpb);

            yield return null;
        }

        // Ensure we land exactly on the base value
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(ID_FresnelPower, fresnelPower);
        _renderer.SetPropertyBlock(_mpb);

        _pulseRoutine = null;
    }
}
