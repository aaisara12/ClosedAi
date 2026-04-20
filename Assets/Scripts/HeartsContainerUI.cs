#nullable enable

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a row of HeartUI instances that reflect the player's current health.
///
/// Setup:
///   1. Place this component on a Canvas panel that has a Horizontal Layout Group.
///   2. Assign playerHealth (the player's Health component).
///   3. Assign heartPrefab (a prefab with a HeartUI component).
///
/// Rules:
///   - Each heart = 1 HP.
///   - At most ONE heart regenerates at a time (slider fills from 0→1).
///   - HP is restored to Health only when regen completes (driven by Health.RegenProgress).
///   - Taking damage destroys the pending heart and removes confirmed hearts for each HP lost.
///   - Flicker animations on dying hearts are never cancelled; they always play to completion.
/// </summary>
public class HeartsContainerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health  playerHealth = null!;
    [SerializeField] private HeartUI heartPrefab  = null!;

    // Active full hearts (HP already granted)
    private readonly List<HeartUI> _hearts = new();

    // The single heart currently filling (HP not yet granted), or null
    private HeartUI? _pendingHeart;

    // -------------------------------------------------------------------------
    //  Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        for (int i = 0; i < playerHealth.CurrentHealth; i++)
            SpawnFullHeart();

        playerHealth.OnDamaged.AddListener(OnDamaged);
        playerHealth.OnHeartRegen.AddListener(OnHeartRegen);
    }

    private void Update()
    {
        float progress = playerHealth.RegenProgress;

        if (progress > 0f)
        {
            // Spawn the pending heart the first frame progress becomes non-zero
            if (_pendingHeart == null)
                _pendingHeart = SpawnPendingHeart();

            _pendingHeart.SetFill(progress);
        }
        else if (_pendingHeart != null)
        {
            // Progress was reset to 0 (e.g. damage taken) — destroy pending heart
            Destroy(_pendingHeart.gameObject);
            _pendingHeart = null;
        }
    }

    private void OnDestroy()
    {
        playerHealth.OnDamaged.RemoveListener(OnDamaged);
        playerHealth.OnHeartRegen.RemoveListener(OnHeartRegen);
    }

    // -------------------------------------------------------------------------
    //  Event handlers
    // -------------------------------------------------------------------------

    private void OnDamaged(int damage)
    {
        // Pending heart is cleaned up in Update() when RegenProgress drops to 0.
        // Remove one confirmed heart per point of damage above current health.
        int targetCount = playerHealth.CurrentHealth;
        while (_hearts.Count > targetCount)
        {
            HeartUI heart = _hearts[_hearts.Count - 1];
            _hearts.RemoveAt(_hearts.Count - 1);
            heart.PlayFlicker();
        }
    }

    private void OnHeartRegen()
    {
        // RegenProgress just reset to 0 and Heal(1) was called inside Health.
        // Promote the pending heart to a confirmed heart.
        if (_pendingHeart != null)
        {
            _pendingHeart.SetFill(1f);
            _hearts.Add(_pendingHeart);
            _pendingHeart = null;
        }
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private HeartUI SpawnFullHeart()
    {
        HeartUI heart = Instantiate(heartPrefab, transform);
        heart.SetFill(1f);
        _hearts.Add(heart);
        return heart;
    }

    private HeartUI SpawnPendingHeart()
    {
        HeartUI heart = Instantiate(heartPrefab, transform);
        heart.SetFill(0f);
        return heart;
    }
}
