using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;

public class InGameMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    public GameObject optionsMenu;
    [SerializeField] public GameObject audioSettings;
    bool isVisible;
    public static bool isGamePaused;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isVisible = false;
    }

    // Update is called once per frame
    void Update()
    {
        // active ou desactive le overlay quand on clique sur escape
        if (Input.GetKeyDown(KeyCode.Escape))
        { 
            isVisible = !isVisible;
            isGamePaused = !isGamePaused;
            panel.SetActive(isVisible);
            optionsMenu.SetActive(isVisible);
            //print("ISVISIBLE" +isVisible);

            if(isVisible == false)
            {
                audioSettings.SetActive(false);
            }

            // met en pause le jeu quand le overlay est actif 
            Time.timeScale = isVisible ? 0f : 1f;
            if (isVisible)
            {
                AkUnitySoundEngine.Suspend();
            }
            else
            {
                AkUnitySoundEngine.WakeupFromSuspend();
            }
        }
    }
}
