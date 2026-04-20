#nullable enable

using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    [SerializeField] private GameObject effectPrefab;

    [SerializeField] private int lifeTimeSeconds = 3;
    
    public void SpawnEffect()
    {
        GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
        Destroy(effect, lifeTimeSeconds);
    }
}
