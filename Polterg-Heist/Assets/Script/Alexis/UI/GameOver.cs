using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : BaseSceneManager
{
    public void Restart()
    {
        string sceneName = PlayerPrefs.GetString("LastScene", "UI_Accueil");
        SceneManager.LoadScene(sceneName);
    }
}
