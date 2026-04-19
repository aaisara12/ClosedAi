#nullable enable

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Wins level when all enemies are defeated. Can be force won (if there's a key enemy that needs to be defeated)
/// </summary>
public class Level : MonoBehaviour
{
    public UnityEvent OnLevelWon;
    
    private bool isLevelWon;
    
    public void Update()
    {
        if (isLevelWon == false && EnemyCounter.EnemyCount == 0)
        {
            WinLevel();
        }
    }

    public void WinLevel()
    {
        isLevelWon = true;
        OnLevelWon.Invoke();
    }
}
