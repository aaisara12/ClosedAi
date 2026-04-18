#nullable enable

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wrapper around Unity's scene management API to provide support for smooth transition animations between scenes
/// and ensure consistent handling of scene loading and unloading.
/// </summary>

// aisara => MonoBehaviour because we may need to deal with animations or coroutines for transitions
public class SceneTransitioner : MonoBehaviour
{
    [Header("Output")] 
    [SerializeField] private LoadingScreenAnimator? loadingScreenAnimator;
    [SerializeField] private float delayBeforeTogglingLoadingScreen = 0.5f;
    
    private Task? ongoingSceneTransitionTask;

    private string? lastSceneLoaded;
    

    // aisara => Don't return bool because it's our responsibility to figure out what to do in a failed scene transition,
    // not the caller's (for example, we could pop a UI that says scene transition failed or raise some events on our end).
    private async Task TransitionToScene(string sceneName)
    {
        loadingScreenAnimator?.FadeInLoadingScreen();
        await Task.Delay((int)(delayBeforeTogglingLoadingScreen * 1000));
        
        var loadNewSceneTask = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)?.AsTask();

        if (loadNewSceneTask == null)
        {
            Debug.LogError($"Failed to load new scene '{sceneName}'");
            return;
        }
        
        if (lastSceneLoaded == null)
        {
            await loadNewSceneTask;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            lastSceneLoaded = sceneName;
            return;
        }
        
        var unloadOldSceneTask = SceneManager.UnloadSceneAsync(lastSceneLoaded)?.AsTask();
        
        if (unloadOldSceneTask == null)
        {
            Debug.LogError($"Failed to unload old scene '{lastSceneLoaded}'");
            return;
        }
        
        await Task.WhenAll(new Task[] { loadNewSceneTask, unloadOldSceneTask });
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        
        loadingScreenAnimator?.FadeOutLoadingScreen();
        await Task.Delay((int)(delayBeforeTogglingLoadingScreen * 1000));
        
        lastSceneLoaded = sceneName;
    }
    
    public bool TryChangeScene(string sceneName)
    {
        if (ongoingSceneTransitionTask is { IsCompleted: false })
        {
            return false;
        }
        
        ongoingSceneTransitionTask = TransitionToScene(sceneName);
        
        return true;
    }

    /// <summary>
    /// Add in a scene that's not intended to replace the current scene, e.g., a UI overlay scene.
    /// </summary>
    /// <param name="sceneName"></param>
    public void AddAuxiliaryScene(string sceneName)
    {
        var loadSceneOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (loadSceneOperation == null)
        {
            Debug.LogError($"Failed to load scene '{sceneName}'");
        }
    }
}
