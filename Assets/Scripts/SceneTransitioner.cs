#nullable enable

using System.Collections;
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
    
    private bool isTransitioning;

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
        if (isTransitioning)
        {
            return false;
        }
        
        StartCoroutine(TransitionToScene(scene));
        
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
    
    private IEnumerator TransitionToScene(SceneReference scene)
    {
        isTransitioning = true;
        
        var sceneName = scene.sceneName;
        
        if (loadingScreenAnimator != null)
            yield return StartCoroutine(loadingScreenAnimator.FadeInLoadingScreenCoroutine());
        yield return new WaitForSeconds(delayBeforeTogglingLoadingScreen);
        
        var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (loadOp == null)
        {
            Debug.LogError($"Failed to load new scene '{sceneName}'");
            isTransitioning = false;
            yield break;
        }

        if (lastSceneLoaded == null)
        {
            var activeScene = SceneManager.GetActiveScene();
            lastSceneLoaded = activeScene.name;
        }

        var unloadOp = SceneManager.UnloadSceneAsync(lastSceneLoaded);
        if (unloadOp == null)
        {
            Debug.LogError($"Failed to unload old scene '{lastSceneLoaded}'");
            isTransitioning = false;
            yield break;
        }

        // Both ops started simultaneously; yield sequentially to wait for both
        yield return loadOp;
        yield return unloadOp;
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        
        if (loadingScreenAnimator != null)
            yield return StartCoroutine(loadingScreenAnimator.FadeOutLoadingScreenCoroutine());
        yield return new WaitForSeconds(delayBeforeTogglingLoadingScreen);
        
        lastSceneLoaded = sceneName;
        isTransitioning = false;
    }
}
