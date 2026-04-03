using UnityEngine;
using UnityEngine.UI;

public class LevelSelect : MonoBehaviour
{
    // Manages the level selection menu

    [SerializeField] private GameObject levelButtons; // Parent object holding all level button children
    private Button[] boutons;

    private void Awake()
    {
        boutons = levelButtons.GetComponentsInChildren<Button>();

        // Get the number of unlocked levels (default = 1)
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);


        for (int i = 0; i < boutons.Length; i++)
        {
            boutons[i].interactable = i < unlockedLevel;
        }
    }
}
