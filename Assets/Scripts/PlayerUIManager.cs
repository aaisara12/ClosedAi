using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{
    [Header("Weapon Swap")]
    [SerializeField] private RectTransform _swordUI;
    [SerializeField] private RectTransform _gunUI;
    [SerializeField] private float _activeScale = 1.2f;
    [SerializeField] private float _inactiveScale = 0.85f;
    [SerializeField] private float _scaleSpeed = 8f;

    [Header("Ammo")]
    [SerializeField] private TMP_Text _ammoText;

    [Header("Dash Cooldown")]
    [SerializeField] private Image _dashCooldownOverlay;
    [SerializeField] private TMP_Text _dashCooldownText;

    [Header("Smoke Cooldown")]
    [SerializeField] private Image _smokeCooldownOverlay;
    [SerializeField] private TMP_Text _smokeCooldownText;

    [Header("Enemies")]
    [SerializeField] private TMP_Text _enemyCountText;

    private void Update()
    {
        if (GameManager.Instance == null) return;

        UpdateWeaponUI();
        UpdateAmmoUI();
        UpdateCooldownUI(_dashCooldownOverlay, _dashCooldownText,
            GameManager.Instance.DashCooldownRemaining, GameManager.Instance.DashCooldownTotal);
        UpdateCooldownUI(_smokeCooldownOverlay, _smokeCooldownText,
            GameManager.Instance.SmokeCooldownRemaining, GameManager.Instance.SmokeCooldownTotal);
        if (_enemyCountText != null)
            _enemyCountText.text = GameManager.Instance.EnemyCount.ToString();
    }

    private void UpdateWeaponUI()
    {
        bool gunEquipped = GameManager.Instance.PistolEquipped;

        Vector3 gunTarget = Vector3.one * (gunEquipped ? _activeScale : _inactiveScale);
        Vector3 swordTarget = Vector3.one * (gunEquipped ? _inactiveScale : _activeScale);

        if (_gunUI != null)
            _gunUI.localScale = Vector3.Lerp(_gunUI.localScale, gunTarget, _scaleSpeed * Time.deltaTime);
        if (_swordUI != null)
            _swordUI.localScale = Vector3.Lerp(_swordUI.localScale, swordTarget, _scaleSpeed * Time.deltaTime);
    }

    private void UpdateAmmoUI()
    {
        if (_ammoText == null) return;
        int loaded = GameManager.Instance.PistolLoaded ? 1 : 0;
        _ammoText.text = $"{loaded}/{GameManager.Instance.PistolAmmo - loaded}";
    }

    private void UpdateCooldownUI(Image overlay, TMP_Text label, float remaining, float total)
    {
        bool onCooldown = remaining > 0f && total > 0f;

        if (overlay != null)
        {
            overlay.gameObject.SetActive(onCooldown);
            if (onCooldown)
                overlay.fillAmount = remaining / total;
        }

        if (label != null)
        {
            label.gameObject.SetActive(onCooldown);
            if (onCooldown)
                label.text = Mathf.CeilToInt(remaining).ToString();
        }
    }
}
