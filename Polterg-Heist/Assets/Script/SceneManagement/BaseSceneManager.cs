using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Enum representing all available scenes in the game
public enum SceneName
{
    MainMenu,
    LevelSelect,
    Controls,
    Story,
    AudioSettings,
    Niveau1,
    Niveau2,
    Niveau3,
    Niveau4,
    Niveau5,
    Niveau6
}

public class BaseSceneManager : MonoBehaviour
{
    // Singleton that managea all scene navigation

    public static BaseSceneManager Instance;

    // Fired exclusively when the application is about to close
    public static event Action OnGameQuit;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Close the Game
    public void QuitGame()
    {
        OnGameQuit?.Invoke();
        Application.Quit();
    }

    // Loads a specific scene by its SceneName enum value
    public void LoadSpecificScene(SceneName scene)
    {
        SceneManager.LoadScene(scene.ToString());
    }

    // Loads the next level in the SceneName enum sequence
    // Falls back to LevelSelect if no next level exists
    public void LoadNextLevel()
    {
        if (Enum.TryParse(SceneManager.GetActiveScene().name, true, out SceneName scene))
        {
            SceneName nextScene = scene + 1;

            if (Enum.IsDefined(typeof(SceneName), nextScene))
            {
                // Resume the game if paused and start the next level
                Time.timeScale = 1f;
                LoadSpecificScene(nextScene);
            }
            else
            {
                // No more level
                LoadSpecificScene(SceneName.LevelSelect);
            }
        }
        else
        {
            Debug.LogError("Scene not found in enum: " + SceneManager.GetActiveScene().name);
            LoadSpecificScene(SceneName.MainMenu);
        }
    }
}
