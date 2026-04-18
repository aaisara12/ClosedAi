using UnityEngine;

public class AnyButtonToContinue : MonoBehaviour
{
    public SceneTransitionRequester sceneTransitionRequester;
    
    // Update is called once per frame
    void Update()
    {
        if (Input.anyKeyDown)
        {
            sceneTransitionRequester.RequestScene();
        }
    }
}
