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
    public float layer1Tiling  = 1.0f;
    public float layer1ScrollX = 0.1f;
    public float layer1ScrollY = 0.2f;
    public Color layer1Color   = new Color(0.2f, 0.6f, 1.0f, 1.0f);

    [Header("Layer 2")]
    public float layer2Tiling  = 1.5f;
    public float layer2ScrollX = -0.15f;
    public float layer2ScrollY = 0.1f;
    public Color layer2Color   = new Color(0.1f, 0.4f, 0.9f, 1.0f);

    [Header("Layer 3")]
    public float layer3Tiling  = 2.5f;
    public float layer3ScrollX = 0.05f;
    public float layer3ScrollY = -0.25f;
    public Color layer3Color   = new Color(0.5f, 0.8f, 1.0f, 1.0f);

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
    private static readonly int ID_Layer1Tiling  = Shader.PropertyToID("_Layer1Tiling");
    private static readonly int ID_Layer1ScrollX = Shader.PropertyToID("_Layer1ScrollX");
    private static readonly int ID_Layer1ScrollY = Shader.PropertyToID("_Layer1ScrollY");
    private static readonly int ID_Layer1Color   = Shader.PropertyToID("_Layer1Color");

    private static readonly int ID_Layer2Tiling  = Shader.PropertyToID("_Layer2Tiling");
    private static readonly int ID_Layer2ScrollX = Shader.PropertyToID("_Layer2ScrollX");
    private static readonly int ID_Layer2ScrollY = Shader.PropertyToID("_Layer2ScrollY");
    private static readonly int ID_Layer2Color   = Shader.PropertyToID("_Layer2Color");

    private static readonly int ID_Layer3Tiling  = Shader.PropertyToID("_Layer3Tiling");
    private static readonly int ID_Layer3ScrollX = Shader.PropertyToID("_Layer3ScrollX");
    private static readonly int ID_Layer3ScrollY = Shader.PropertyToID("_Layer3ScrollY");
    private static readonly int ID_Layer3Color   = Shader.PropertyToID("_Layer3Color");

    private static readonly int ID_FresnelPower  = Shader.PropertyToID("_FresnelPower");
    private static readonly int ID_FresnelColor  = Shader.PropertyToID("_FresnelColor");

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

        _mpb.SetFloat(ID_Layer1Tiling,  layer1Tiling);
        _mpb.SetFloat(ID_Layer1ScrollX, layer1ScrollX);
        _mpb.SetFloat(ID_Layer1ScrollY, layer1ScrollY);
        _mpb.SetColor(ID_Layer1Color,   layer1Color);

        _mpb.SetFloat(ID_Layer2Tiling,  layer2Tiling);
        _mpb.SetFloat(ID_Layer2ScrollX, layer2ScrollX);
        _mpb.SetFloat(ID_Layer2ScrollY, layer2ScrollY);
        _mpb.SetColor(ID_Layer2Color,   layer2Color);

        _mpb.SetFloat(ID_Layer3Tiling,  layer3Tiling);
        _mpb.SetFloat(ID_Layer3ScrollX, layer3ScrollX);
        _mpb.SetFloat(ID_Layer3ScrollY, layer3ScrollY);
        _mpb.SetColor(ID_Layer3Color,   layer3Color);

        _mpb.SetFloat(ID_FresnelPower,  fresnelPower);
        _mpb.SetColor(ID_FresnelColor,  fresnelColor);

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

