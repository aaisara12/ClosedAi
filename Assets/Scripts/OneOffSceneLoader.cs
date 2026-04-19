#nullable enable

using UnityEngine;
using UnityEngine.SceneManagement;

public class OneOffSceneLoader : MonoBehaviour
{
    [SerializeField] SceneReference sceneToLoad;
    public void Awake()
    {
        SceneManager.LoadScene(sceneToLoad.sceneName, LoadSceneMode.Additive);
    }
}
