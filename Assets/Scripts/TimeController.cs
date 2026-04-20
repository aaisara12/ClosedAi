#nullable enable

using UnityEngine;

public class TimeController : MonoBehaviour
{
    public void SetTimeScale(float newTimeScale)
    {
        Time.timeScale = newTimeScale;
    }
}
