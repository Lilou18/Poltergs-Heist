using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    ScoreUI scoreUI;

    private int deaths = 0;
    private float timer;
    private int collectedItems = 0;

    private int currentLevel = 1;

    // Preset data to calculate score on each level
    private readonly Dictionary<int, DataLevel> levelCriteria = new Dictionary<int, DataLevel>()
    {
        //                                     1S                       2S                       3S  1S 2S 3S
        {1, new DataLevel(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(2), 3, 2, 1)},  // Level 1 Data
        {2, new DataLevel(TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(6), TimeSpan.FromMinutes(4), 3, 2, 1)},  // Level 2 Data
        {3, new DataLevel(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(4), 3, 2, 1)},  // Level 3 Data
        {4, new DataLevel(TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(3), 3, 2, 1)},  // Level 4 Data
        {5, new DataLevel(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(5), 3, 2, 1)},  // Level 5 Data
        {6, new DataLevel(TimeSpan.FromMinutes(9), TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(5), 3, 2, 1)}   // Level 6 Data
    };

    //public event Action<TimeSpan, int> OnShowTimer;
    //public event Action<int, int> OnShowDeaths;
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
        timer = Time.time;
        //Debug.Log($"Niveau actuel d?tect? : {currentLevel}");
        //CalculateScore();
    }

    private void Update()
    {
        //print("TEST PAUSE!!!");
        //TimeSpan elapsedTime = TimeSpan.FromSeconds(Time.time - timer);
        //timerTest.text = elapsedTime.ToString(@"mm\:ss");
    }

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

        collectedItems = InventorySystem.Instance.StolenItemList.Count;
        collectedItemStars = collectedItems;
        OnShowScoreBoard?.Invoke(elapsedTime, timeStars, deaths, deathsStars, collectedItems, collectedItemStars);

    }

    private int CalculateStars<T>(T value, T oneStarThreshold, T twoStarThreshold, T threeStarThreshold) where T : IComparable<T>
    {
        // Compare value to the thresholds
        if (value.CompareTo(threeStarThreshold) <= 0) return 3;  
        if (value.CompareTo(twoStarThreshold) <= 0) return 2;    
        if (value.CompareTo(oneStarThreshold) <= 0) return 1;    
        return 0;
    }


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
            Debug.Log("Niveau actuel ne contient pas de chiffre!!!");
        }
    }

    // Count the number of deaths of the player
    public void AddDeath()
    {
        deaths++;
        print("DEATHS " + deaths);
    }

    public void GoBackLevelMenu()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    public void GoNextLevel()
    {
        Scene curScene = SceneManager.GetActiveScene();
        Time.timeScale = 1f;
        SceneManager.LoadScene(curScene.buildIndex + 1);
    }

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


