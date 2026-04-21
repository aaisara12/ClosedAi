#nullable enable

using System.Globalization;
using TMPro;
using UnityEngine;

public class TimeRemainingUI : MonoBehaviour
{
    public TMP_Text text;
    public void Update()
    {
        var gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            return;
        }

        text.enabled = gameManager.HasCountdown;
        text.text = gameManager.TimeRemaining.ToString("F2", CultureInfo.InvariantCulture) + "s";
    }
}
