using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuButton : MonoBehaviour
{
    // Handles button actions specific to the pause menu.

    // Resets audio state, unpauses the game and navigates back to the Main Menu
    public void BackMainMenu()
    {
        AudioManager.Instance.PauseAllAudio();
        AudioManager.Instance.ResumeAudio();
        BaseSceneManager.Instance.LoadSpecificScene(SceneName.MainMenu);
        // Ensure game is unpaused
        Time.timeScale = 1f;
    }

    // Restarts the current level by reloading the active scene
    public void Restart()
    {
        // Unpause the game
        Time.timeScale = 1f;
        AudioManager.Instance.PauseAllAudio();
        AudioManager.Instance.ResumeAudio();

        // Parse the active scene name to its matching SceneName enum value
        if (Enum.TryParse(SceneManager.GetActiveScene().name, true, out SceneName scene))
        {
            BaseSceneManager.Instance.LoadSpecificScene(scene);
        }
        else
        {
            Debug.LogError("Scene not found in enum: " + SceneManager.GetActiveScene().name);
            BaseSceneManager.Instance.LoadSpecificScene(SceneName.MainMenu);
        }
    }
}
