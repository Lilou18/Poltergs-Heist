using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Victory : MonoBehaviour
{
    private int numberOfLevels = 6;

    // The player reached the end of the level
    // Pause the game, unlock new level and calculate the score
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {  
            UnlockNewLevel();
            ScoreManager.Instance.CalculateScore();
            Time.timeScale = 0f;          
        } 
    }

    // Unlock the new level if there is one
    void UnlockNewLevel()
    {
        int unlocked = PlayerPrefs.GetInt("UnlockedLevel", 1);

        if(unlocked < numberOfLevels)
        {
            PlayerPrefs.SetInt("UnlockedLevel", PlayerPrefs.GetInt("UnlockedLevel", 1) + 1);
            PlayerPrefs.Save();
        }
    }
}
