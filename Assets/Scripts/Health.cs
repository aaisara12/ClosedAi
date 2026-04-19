#nullable enable

using UnityEngine;
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

    public int CurrentHealth = 100;
    public int MaxHealth = 100;
    
    public void TakeDamage(int damage)
    {
        OnDamaged.Invoke(damage);
        
        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
        OnCurrentHealthChanged.Invoke(CurrentHealth);

        if (CurrentHealth <= 0)
        {
            OnDeath.Invoke();
        }
    }
}
