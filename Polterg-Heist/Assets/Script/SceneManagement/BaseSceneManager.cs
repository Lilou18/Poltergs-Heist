using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class BaseSceneManager : MonoBehaviour
{
    public void Jouer()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Retour()
    {
        SceneManager.LoadScene("UI_Accueil");
    }
}
