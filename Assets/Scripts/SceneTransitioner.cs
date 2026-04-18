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
    public static SceneTransitioner? Instance { get; private set; }

    [Header("Output")] 
    [SerializeField] private LoadingScreenAnimator? loadingScreenAnimator;
    [SerializeField] private float delayBeforeTogglingLoadingScreen = 0.5f;
    
    private Task? ongoingSceneTransitionTask;

    private string? lastSceneLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    public bool TryChangeScene(SceneReference scene)
    {
        if (ongoingSceneTransitionTask is { IsCompleted: false })
        {
            return false;
        }
        
        ongoingSceneTransitionTask = TransitionToScene(scene);
        
        return true;
    }

    /// <summary>
    /// Add in a scene that's not intended to replace the current scene, e.g., a UI overlay scene.
    /// </summary>
    /// <param name="scene"></param>
    public void AddAuxiliaryScene(SceneReference scene)
    {
        var sceneName = scene.sceneName;
        
        var loadSceneOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (loadSceneOperation == null)
        {
            Debug.LogError($"Failed to load scene '{sceneName}'");
        }
    }
    
    private async Task<bool> TransitionToScene(SceneReference scene)
    {
        var sceneName = scene.sceneName;
        
        loadingScreenAnimator?.FadeInLoadingScreen();
        await Task.Delay((int)(delayBeforeTogglingLoadingScreen * 1000));
        
        var loadNewSceneTask = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)?.AsTask();

        if (loadNewSceneTask == null)
        {
            Debug.LogError($"Failed to load new scene '{sceneName}'");
            return false;
        }
        
        if (lastSceneLoaded == null)
        {
            await loadNewSceneTask;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            lastSceneLoaded = sceneName;
            return false;
        }
        
        var unloadOldSceneTask = SceneManager.UnloadSceneAsync(lastSceneLoaded)?.AsTask();
        
        if (unloadOldSceneTask == null)
        {
            Debug.LogError($"Failed to unload old scene '{lastSceneLoaded}'");
            return false;
        }
        
        await Task.WhenAll(new Task[] { loadNewSceneTask, unloadOldSceneTask });
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        
        loadingScreenAnimator?.FadeOutLoadingScreen();
        await Task.Delay((int)(delayBeforeTogglingLoadingScreen * 1000));
        
        lastSceneLoaded = sceneName;
        return true;
    }
}
