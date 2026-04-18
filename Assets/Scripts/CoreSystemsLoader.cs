#nullable enable

using UnityEngine;
using UnityEngine.SceneManagement;

public class CoreSystemsLoader : MonoBehaviour
{
    public void Awake()
    {
        SceneManager.LoadScene("CoreSystems", LoadSceneMode.Additive);
    }
}
