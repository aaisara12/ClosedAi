#nullable enable

using UnityEngine;

public class EnemyCounter : MonoBehaviour
{
    // No multithreading, so no need for locks or atomic operations here.
    public static int EnemyCount;

    private void OnEnable()
    {
        EnemyCount++;
    }

    private void OnDisable()
    {
        EnemyCount--;
    }
}
