#nullable enable

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Wins level when all enemies are defeated. Can be force won (if there's a key enemy that needs to be defeated)
/// </summary>
public class Level : MonoBehaviour
{
    [SerializeField] private UnityEvent OnLevelWon;
    [SerializeField] private UnityEvent OnLevelRestarted;
    [SerializeField] private UnityEvent OnLevelLost;
    
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

    public void RestartLevel()
    {
        OnLevelRestarted.Invoke();
    }

    public void LoseLevel()
    {
        OnLevelLost.Invoke();
    }
}
