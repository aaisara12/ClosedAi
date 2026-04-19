#nullable enable

using UnityEngine;

public class TriggerDamager : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private string specificDamageTag = string.Empty;
    
    public void OnTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(specificDamageTag) || other.CompareTag(specificDamageTag))
        {
            other.GetComponent<Health>()?.TakeDamage(damageAmount);
        }
    }
}
