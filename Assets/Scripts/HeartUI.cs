#nullable enable

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the root of a Heart UI prefab.
/// The prefab should have:
///   - heartImage   : the base heart icon (always visible)
///   - regenSlider  : a Slider (Min=0, Max=1, Interactable=false, Handle hidden)
///                    used to display regen progress. Value is driven externally each frame.
/// </summary>
public class HeartUI : MonoBehaviour
{
    [SerializeField] private Image  heartImage  = null!;
    [SerializeField] private Image refillImage = null!;
    [SerializeField] private Slider regenSlider = null!;

    // How many times the heart flickers before fading out
    [SerializeField] private int   flickerCount    = 4;
    [SerializeField] private float flickerDuration = 0.08f;

    // -------------------------------------------------------------------------
    //  Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets the regen slider value. Pass 1 for a full heart, 0 for empty.
    /// </summary>
    public void SetFill(float amount)
    {
        regenSlider.value = Mathf.Clamp01(amount);

        heartImage.enabled = false;
        refillImage.enabled = true;
        regenSlider.gameObject.SetActive(true);
        
        if (Mathf.Approximately(amount, 1) || Mathf.Approximately(amount, 0))
        {
            heartImage.enabled = true;
        }
    }

    /// <summary>
    /// Plays a flicker animation then destroys this GameObject when done.
    /// </summary>
    public void PlayFlicker(Action? onComplete = null)
    {
        refillImage.enabled = false;
        
        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < flickerCount; i++)
        {
            seq.Append(heartImage.DOFade(0f, flickerDuration));
            seq.Append(heartImage.DOFade(1f, flickerDuration));
        }

        seq.Append(heartImage.DOFade(0f, flickerDuration * 2f));
        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
            Destroy(gameObject);
        });
    }
}
