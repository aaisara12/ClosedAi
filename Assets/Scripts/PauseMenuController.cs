#nullable enable

using System;
using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject? pauseMenuUI;

    private bool isPaused;

    public void SetPauseState(bool isPaused)
    {
        if (pauseMenuUI == null)
        {
            return;
        }
        
        this.isPaused = isPaused;
        
        pauseMenuUI.SetActive(isPaused);
        Time.timeScale = isPaused ? 0 : 1;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPauseState(!isPaused);
        }
    }
}
