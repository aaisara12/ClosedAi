#nullable enable

using UnityEngine;

[CreateAssetMenu(menuName = "Scene Transition Requester")]
public class SceneTransitionRequester : ScriptableObject
{
    public SceneReference sceneToRequest = new SceneReference();

    public void RequestScene()
    {
        if (SceneTransitioner.Instance == null)
        {
            Debug.LogWarning("Scene Transitioner not found");
            return;
        }

        if (!SceneTransitioner.Instance.TryChangeScene(sceneToRequest))
        {
            Debug.LogWarning($"Scene Transitioner failed to load scene {sceneToRequest.sceneName}");
        }
    }
}
