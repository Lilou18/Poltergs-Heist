using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class UIButtonHandler : MonoBehaviour
{
    // Attach to any Button that requires to change a scene or app control.

    // Target scene to load
    [SerializeField] private SceneName sceneName;

    // Loads the scene assigned in the Inspector
    public void LoadScene()
    {
        BaseSceneManager.Instance.LoadSpecificScene(sceneName);
    }

    // Loads the next level
    public void LoadNextLevel()
    {
        BaseSceneManager.Instance.LoadNextLevel();
    }

    // Quit the game
    public void QuitGame()
    {
        BaseSceneManager.Instance.QuitGame();
    }

    // Clears all saved PlayerPrefs data
    // intended for playtesting purposes only
    public void ClearPrefs()
    {
        PlayerPrefs.DeleteAll();
    }
}
