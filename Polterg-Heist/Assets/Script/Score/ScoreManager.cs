using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    //Tracks the player's performance during a level (time, deaths, collected items)
    // and calculates a star rating for each category when the level ends.



    public static ScoreManager Instance { get; private set; }   // Singleton

    private ScoreUI scoreUI;                                    // Reference to the UI component that displays the scoreboard

    // Tracked Stats
    private int deaths = 0;                                     // Total number of times the player has died in this level
    private float timer;                                        // Time spent to finish the level
    private int collectedItems = 0;                             // Number of items collected by the player

    private int currentLevel = 1;                               // Index of the current level, parsed from the scene name

    // Star thresholds for each level, keyed by level index.
    // Each entry defines the time and death count required to earn 1, 2, or 3 stars for their specific category.
    private readonly Dictionary<int, DataLevel> levelCriteria = new Dictionary<int, DataLevel>()
    {
        //                                     1S                       2S                       3S  1S 2S 3S
        {1, new DataLevel(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(2), 3, 2, 1)},  // Level 1 Data
        {2, new DataLevel(TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(6), TimeSpan.FromMinutes(4), 3, 2, 1)},  // Level 2 Data
        {3, new DataLevel(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(4), 3, 2, 1)}, // Level 3 Data
        {4, new DataLevel(TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(3), 3, 2, 1)},  // Level 4 Data
        {5, new DataLevel(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(5), 3, 2, 1)}, // Level 5 Data
        {6, new DataLevel(TimeSpan.FromMinutes(9), TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(5), 3, 2, 1)}   // Level 6 Data
    };

    // Fired when the score is ready to be displayed.
    // Parameters: elapsed time, time stars, death count, death stars, collected items, item stars.
    public event Action<TimeSpan, int, int, int, int, int> OnShowScoreBoard;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
        
    void Start()
    {
        scoreUI = GetComponent<ScoreUI>();
        SetCurrentLevel();
        timer = Time.time;  // Start the level timer
    }

    // Called when the player completes the level.
    // Computes elapsed time and star ratings for each category, and then call the event to display the UI.
    public void CalculateScore()
    {
        scoreUI.ShowScorePanel();
        this.GetComponentInChildren<UIFaceAnimationBehavior>().transform.parent.parent.GetComponent<Animator>().SetTrigger("IsLevelDone");
        if (!levelCriteria.ContainsKey(currentLevel))
        {
            Debug.LogError("No criteria for this level");
        }

        int timeStars = 0;
        int deathsStars = 0;
        int collectedItemStars = 0;

        DataLevel dataLevel = levelCriteria[currentLevel];
        TimeSpan elapsedTime = TimeSpan.FromSeconds(Time.time - timer);

        timeStars = CalculateStars(elapsedTime, dataLevel.time1Star, dataLevel.time2Star, dataLevel.time3Star);
        deathsStars = CalculateStars(deaths, dataLevel.death1Star, dataLevel.death2Star, dataLevel.death3Star);

        // Collected items count directly as stars (1 item = 1 star, capped at 3)
        collectedItems = InventorySystem.Instance.StolenItemList.Count;
        collectedItemStars = collectedItems;
        OnShowScoreBoard?.Invoke(elapsedTime, timeStars, deaths, deathsStars, collectedItems, collectedItemStars);

    }

    // Generic star calculator: compares a value against three thresholds and returns 0–3 stars.
    // Works for comparable type such has time and deaths.
    // If the value is lower then the 3 star threshold, returns 3 stars.
    private int CalculateStars<T>(T value, T oneStarThreshold, T twoStarThreshold, T threeStarThreshold) where T : IComparable<T>
    {
        // Compare value to the thresholds
        if (value.CompareTo(threeStarThreshold) <= 0) return 3;  
        if (value.CompareTo(twoStarThreshold) <= 0) return 2;    
        if (value.CompareTo(oneStarThreshold) <= 0) return 1;    
        return 0;
    }

    // Extracts the level number from the active scene name.
    private void SetCurrentLevel()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Match match = Regex.Match(sceneName, @"\d+");

        if (match.Success)
        {
            currentLevel = int.Parse(match.Value);
        }
        else
        {
            currentLevel = 1;
            Debug.Log("This level has no number");
        }
    }

    // Increments the death counter. Called by SuspicionManager when the player dies.
    public void AddDeath()
    {
        deaths++;
    }

    // Inner Class
    // Holds the star thresholds for a single level.
    private class DataLevel
    {
        public TimeSpan time1Star, time2Star, time3Star;
        public int death1Star, death2Star, death3Star;

        public DataLevel (TimeSpan time1Star, TimeSpan time2Star, TimeSpan time3Star, int death1Star, int death2Star, int death3Star)
        {
            this.time1Star = time1Star;
            this.time2Star = time2Star;
            this.time3Star = time3Star;
            this.death1Star = death1Star;
            this.death2Star = death2Star;
            this.death3Star = death3Star;
        }
    }

}


