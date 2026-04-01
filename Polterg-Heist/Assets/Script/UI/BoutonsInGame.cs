using UnityEngine;
using UnityEngine.SceneManagement;

public class BoutonsInGame : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Accueil()
    {
        AkUnitySoundEngine.StopAll();
        AkUnitySoundEngine.WakeupFromSuspend();
        SceneManager.LoadScene("UI_Accueil");
    }

    public void Recommencer()
    {
        //Remet le temps actif si jamais
        Time.timeScale = 1f;
        AkUnitySoundEngine.StopAll();
        AkUnitySoundEngine.WakeupFromSuspend();
        //recommence la scène
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
