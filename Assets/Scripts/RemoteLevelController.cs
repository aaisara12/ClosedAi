#nullable enable

using UnityEngine;

public class RemoteLevelController : MonoBehaviour
{
    public void RestartLevel()
    {
        var level = FindAnyObjectByType<Level>();
        level.RestartLevel();
    }
    
    public void WinLevel()
    {
        var level = FindAnyObjectByType<Level>();
        level.WinLevel();
    }

    public void LoseLevel()
    {
        var level = FindAnyObjectByType<Level>();
        level.LoseLevel();
    }
}
