using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelect : BaseSceneManager
{
    // Manages the level selection menu

    [SerializeField] private GameObject levelButtons; // Parent object holding all level button children
    private Button[] boutons;
    
    private bool isLoading = false; // Prevents multiple clicks while loading a scene

    private void Awake()
    {
        ButtonsToArray();

        // Get the number of unlocked levels (default = 1)
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);


        for (int i = 0; i < boutons.Length; i++)
        {
            boutons[i].interactable = i < unlockedLevel;
        }
    }

    // Loads the selected level when a button is clicked
    public void OpenSelectedLevel(int niveauId)
    {
        // Prevent multiple clicks
        if (isLoading) return;
        isLoading = true;

        string nomNiveau = "Niveau" + niveauId;

        // Load the selected level
        SceneManager.LoadScene(nomNiveau);

        // Ensure the game is resume in case it was paused
        Time.timeScale = 1f;
    }

    // Converts all children of levelButtons into a Button array
    void ButtonsToArray()
    {
        int childCount = levelButtons.transform.childCount;
        boutons = new Button[childCount];
        for (int i = 0; i < childCount; i++)
        {
            boutons[i] = levelButtons.transform.GetChild(i).gameObject.GetComponent<Button>();
        }
    }
}
