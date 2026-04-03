using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;

public class PauseMenu : MonoBehaviour
{
    // Controls the pause menu visibility and game pause state.


    [SerializeField] GameObject panel; // Root pause menu panel
    [SerializeField] private GameObject optionsMenu; // Options sub-menu panel
    [SerializeField] private GameObject audioSettings; // Audio settings sub-menu panel

    private bool isVisible;
    public static bool isGamePaused;

    void Start()
    {
        isVisible = false;
    }

    void Update()
    {
        // Toggle the pause menu when the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // Toggles the pause state, updates UI panels and controls audio and time scale
    private void TogglePause()
    {
        isVisible = !isVisible;
        isGamePaused = !isGamePaused;
        panel.SetActive(isVisible);
        optionsMenu.SetActive(isVisible);

        // Hide audio settings when closing the pause menu
        if (!isVisible) audioSettings.SetActive(false);

        // Pause the game when the pause menu is displayed 
        Time.timeScale = isVisible ? 0f : 1f;

        // Stop the sound when the game is on pause
        if (isVisible)
        {
            AudioManager.Instance.PauseAudio();
        }
        else
        {
            AudioManager.Instance.ResumeAudio();
        }
    }
}
