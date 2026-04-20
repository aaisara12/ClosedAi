using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Pistol), typeof(MeleeAttack))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Transform _gunArm;
    [SerializeField] private Transform _swordArm;
    [SerializeField] private Animator _gunAnimator;

    [Header("Swap")]
    [SerializeField] private float _swapDuration = 0.18f;
    [SerializeField] private Vector3 _hideOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Recoil")]
    [SerializeField] private float _recoilAngle = 6f;
    [SerializeField] private float _recoilDuration = 0.05f;
    [SerializeField] private float _recoilRecovery = 0.12f;

    [Header("Reload")]
    [SerializeField] private float _reloadAnimDuration = 3.35f;

    [Header("Sword Attack")]
    [SerializeField] private float _swordDipDuration = 0.1f;

    private Pistol _pistol;
    private MeleeAttack _melee;
    private Vector3 _gunArmReadyPos;
    private Vector3 _swordArmReadyPos;
    private Coroutine _swapCoroutine;
    private Coroutine _recoilCoroutine;
    private bool _pendingReloadAnim;

    private void Awake()
    {
        _pistol = GetComponent<Pistol>();
        _melee = GetComponent<MeleeAttack>();
        if (_gunArm != null) _gunArmReadyPos = _gunArm.localPosition;
        if (_swordArm != null) _swordArmReadyPos = _swordArm.localPosition;
    }

    private void Start()
    {
        bool gunEquipped = _pistol != null && _pistol.IsEquipped;
        _gunArm?.gameObject.SetActive(gunEquipped);
        _swordArm?.gameObject.SetActive(!gunEquipped);
    }

    private void OnEnable()
    {
        if (_pistol != null)
        {
            _pistol.OnEquipChanged += HandleEquipChanged;
            _pistol.OnFired += HandleFired;
            _pistol.OnReloadStarted += HandleReloadStarted;
        }
        if (_melee != null)
            _melee.OnAttacked += HandleSwordAttacked;
    }

    private void OnDisable()
    {
        if (_pistol != null)
        {
            _pistol.OnEquipChanged -= HandleEquipChanged;
            _pistol.OnFired -= HandleFired;
            _pistol.OnReloadStarted -= HandleReloadStarted;
        }
        if (_melee != null)
            _melee.OnAttacked -= HandleSwordAttacked;
    }

    private void HandleEquipChanged(bool gunEquipped)
    {
        if (_swapCoroutine != null) StopCoroutine(_swapCoroutine);
        _swapCoroutine = StartCoroutine(SwapCoroutine(gunEquipped));
    }

    private void HandleFired()
    {
        if (_recoilCoroutine != null) StopCoroutine(_recoilCoroutine);
        AudioSystem.Play(AudioSystem.Sound.Pistol);
        _recoilCoroutine = StartCoroutine(RecoilCoroutine());
    }

    private void HandleReloadStarted()
    {
        if (_gunAnimator == null) return;
        if (_swapCoroutine != null)
        {
            _pendingReloadAnim = true;
            return;
        }
        PlayReloadAnimation();
    }

    private void PlayReloadAnimation()
    {
        _gunAnimator.SetFloat("ReloadSpeed", _reloadAnimDuration / _pistol.ReloadTime);
        _gunAnimator.Play("Reload");
        AudioSystem.Play(AudioSystem.Sound.Reload);
    }

    private void HandleSwordAttacked() => StartCoroutine(SwordDipCoroutine());

    private IEnumerator SwapCoroutine(bool toGun)
    {
        Transform hideArm = toGun ? _swordArm : _gunArm;
        Transform showArm = toGun ? _gunArm : _swordArm;
        Vector3 hideReadyPos = toGun ? _swordArmReadyPos : _gunArmReadyPos;
        Vector3 showReadyPos = toGun ? _gunArmReadyPos : _swordArmReadyPos;

        if (hideArm != null)
        {
            yield return MoveArm(hideArm, hideArm.localPosition, hideReadyPos + _hideOffset, _swapDuration);
            hideArm.gameObject.SetActive(false);
        }

        if (showArm != null)
        {
            showArm.localPosition = showReadyPos + _hideOffset;
            showArm.gameObject.SetActive(true);
            yield return MoveArm(showArm, showArm.localPosition, showReadyPos, _swapDuration);
        }

        _swapCoroutine = null;

        if (toGun && _pendingReloadAnim)
        {
            _pendingReloadAnim = false;
            PlayReloadAnimation();
        }
    }

    private IEnumerator RecoilCoroutine()
    {
        if (_gunArm == null) yield break;
        Quaternion baseRot = _gunArm.localRotation;
        Quaternion kickRot = baseRot * Quaternion.AngleAxis(-_recoilAngle, Vector3.right);

        yield return LerpRotation(_gunArm, baseRot, kickRot, _recoilDuration);
        yield return LerpRotation(_gunArm, _gunArm.localRotation, baseRot, _recoilRecovery);
        _recoilCoroutine = null;
    }

    private IEnumerator SwordDipCoroutine()
    {
        if (_swordArm == null) yield break;
        AudioSystem.Play(AudioSystem.Sound.Sword);
        Vector3 startPos = _swordArm.localPosition;
        Vector3 dipPos = _swordArmReadyPos + _hideOffset;
        yield return MoveArm(_swordArm, startPos, dipPos, _swordDipDuration / 2);
        yield return MoveArm(_swordArm, _swordArm.localPosition, _swordArmReadyPos, _swordDipDuration);
    }

    private IEnumerator MoveArm(Transform arm, Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f) { arm.localPosition = to; yield break; }
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Clamp01(t + Time.deltaTime / duration);
            arm.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }

    private IEnumerator LerpRotation(Transform target, Quaternion from, Quaternion to, float duration)
    {
        if (duration <= 0f) { target.localRotation = to; yield break; }
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Clamp01(t + Time.deltaTime / duration);
            target.localRotation = Quaternion.Lerp(from, to, t);
            yield return null;
        }
    }
}
