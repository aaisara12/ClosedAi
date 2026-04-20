#nullable enable

using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    // Invoked with the amount of damage taken
    public UnityEvent<int> OnDamaged;

    // Invoked with current health
    public UnityEvent<int> OnCurrentHealthChanged;

    // Invoked with max health
    public UnityEvent<int> OnMaxHealthChanged;
    public UnityEvent OnDeath;

    /// <summary>Fired each time regen completes one full heart.</summary>
    public UnityEvent OnHeartRegen = new();

    public int CurrentHealth = 3;
    public int MaxHealth = 3;
    public bool canRegen = false;

    [SerializeField] public bool canShield = false;
    [SerializeField] private float _shieldCD = .5f;
    public GameObject Shield;
    public bool hasShield = false;
    private bool _isShielding = false;
    
    [Header("Regen Settings")]
    [Tooltip("Seconds after last damage before regeneration begins.")]
    [SerializeField] private float regenDelay = 5f;
    [Tooltip("Seconds it takes to fill one heart.")]
    [SerializeField] private float regenDuration = 2f;

    /// <summary>
    /// 0 while idle or in the delay window; rises from 0→1 as a heart fills;
    /// resets to 0 on damage or when already at max health.
    /// </summary>
    public float RegenProgress { get; private set; }

    private float _timeSinceLastDamage = 0f;
    private bool  _regenActive = false;
    private SignalManager signal;

    // -------------------------------------------------------------------------
    //  Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        signal = GetComponentInChildren<SignalManager>();
        if (canShield && signal != null)
        {
            hasShield = true;
            signal.OnFullyIsolated += HandleFullyIsolated;
        }
    }

    private void HandleFullyIsolated()
    {
        hasShield = false;
    }


    private void Update()
    {
        if (Shield != null)
        {
            Shield.SetActive(hasShield);
        }

        if (canShield && !_isShielding && !hasShield)
        {
            StartCoroutine(shieldCoro());
        }
        
        if (CurrentHealth >= MaxHealth)
        {
            RegenProgress = 0f;
            _regenActive  = false;
            return;
        }

        if (canRegen == false)
        {
            return;
        }

        _timeSinceLastDamage += Time.deltaTime;

        if (!_regenActive)
        {
            if (_timeSinceLastDamage >= regenDelay)
                _regenActive = true;
            else
                return;
        }

        RegenProgress += Time.deltaTime / regenDuration;

        if (RegenProgress >= 1f)
        {
            RegenProgress = 0f;
            Heal(1);
            OnHeartRegen.Invoke();

            // If still below max, begin filling the next heart immediately
            _regenActive = CurrentHealth < MaxHealth;
        }
    }

    // -------------------------------------------------------------------------
    //  Public API
    // -------------------------------------------------------------------------

    public void TakeDamage(int damage)
    {
        if (hasShield)
        {
            hasShield = false;
            return;
        }

        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
        OnCurrentHealthChanged.Invoke(CurrentHealth);

        // Reset regen
        RegenProgress        = 0f;
        _regenActive         = false;
        _timeSinceLastDamage = 0f;

        OnDamaged.Invoke(damage);

        if (CurrentHealth <= 0)
        {
            OnDeath.Invoke();
        }
    }

    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        OnCurrentHealthChanged.Invoke(CurrentHealth);
    }

    IEnumerator shieldCoro()
    {
        Debug.Log("meow");
        _isShielding = true;
        yield return new WaitForSeconds(_shieldCD);
        while (signal.IsDisabled || signal.GetDirectNeighbours().Count == 0)
        {
            yield return null;
        }
        hasShield = true;
        _isShielding = false;
    }
}
