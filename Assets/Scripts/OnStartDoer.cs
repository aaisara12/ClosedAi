#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class OnStartDoer : MonoBehaviour
{
    public UnityEvent OnStart;
    public int delay = 3;
    
    private void Start()
    {
        StartCoroutine(DoSomething(delay));
    }

    private IEnumerator DoSomething(int delay)
    {
        yield return new WaitForSeconds(delay);
        OnStart.Invoke();
    }
}
