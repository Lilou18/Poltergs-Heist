using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    // Handles the UI for the scoreboard

    [Header ("ScoreBoard UI Gameobjects")]
    [SerializeField] Canvas canvasScore;                            // Canvas containing the entire score screen, enabled when the level ends
    [SerializeField] GameObject scorePanel;                         // GameObject containing all score UI elements

    [SerializeField] GameObject timerGroup;                         // UI group for the time stat text
    [SerializeField] GameObject deathsGroup;                        // UI group for the death count stat text
    [SerializeField] GameObject collectedItemGroup;                 // UI group for the collected items stat text

    [SerializeField] TMP_Text numberOfDeathsTxt;                    // UI text displaying the death count value
    [SerializeField] TMP_Text numberOfTimeTxt;                      // UI text displaying the elapsed time value
    [SerializeField] TMP_Text collectedItemTxt;                     // UI text displaying the collected items count value

    [SerializeField] GameObject[] starsImageTimer;                  // Star icons for the time category
    [SerializeField] GameObject[] starsImageDeaths;                 // Star icons for the deaths category
    [SerializeField] GameObject[] starsImageItemCollected;          // Star icons for the collected items category

    // Sound Event
    [Header ("Sound Variables")]
    [SerializeField] protected AK.Wwise.Event victorySoundEvent;    // Music when the level is finished
    [SerializeField] protected AK.Wwise.Event winSoundEvent;
    [SerializeField] protected AK.Wwise.Event statsSoundEvent;      // Sound Event fired when stats are revealed
    [SerializeField] protected AK.Wwise.Event[] startSoundEvent;    // Sound Event fired when each star appears
    void Start()
    {
        // Subscribe to the score event so the UI updates automatically when the level ends
        ScoreManager.Instance.OnShowScoreBoard += ShowScoreBoard;
    }

    // Enables the score canvas and plays victory sounds.
    public void ShowScorePanel()
    {
        canvasScore.enabled = true;
        AkUnitySoundEngine.StopAll();
        winSoundEvent.Post(gameObject);
        victorySoundEvent.Post(gameObject);
    }

    // Display the scoreBoard.
    // Delegates to the coroutine so each stat can be revealed with delays.
    private void ShowScoreBoard(TimeSpan time, int numberStarsTime, int numberDeaths, int numberStarsDeaths, int collectedItems, int numberStarsItems)
    {
        StartCoroutine(ShowScoreBoardCoroutine(time, numberStarsTime, numberDeaths, numberStarsDeaths, collectedItems, numberStarsItems));
    }

    // Manage the ScoreBoard Animation and display.
    private IEnumerator ShowScoreBoardCoroutine(TimeSpan time, int numberStarsTime, int numberDeaths, int numberStarsDeaths, int collectedItems, int numberStarsItems)
    {
        // Wait for the level-end animation to finish
        yield return new WaitForSecondsRealtime(1.5f);


        numberOfTimeTxt.text = time.ToString(@"mm\:ss");
        yield return StartCoroutine(ShowText(timerGroup));
        yield return StartCoroutine(ShowStars(numberStarsTime, starsImageTimer));

        numberOfDeathsTxt.text = numberDeaths.ToString();
        yield return StartCoroutine(ShowText(deathsGroup));
        yield return (StartCoroutine(ShowStars(numberStarsDeaths, starsImageDeaths)));

        collectedItemTxt.text = collectedItems.ToString();
        yield return StartCoroutine(ShowText(collectedItemGroup));
        yield return (StartCoroutine(ShowStars(numberStarsItems, starsImageItemCollected)));
    }

    // Reveals the right amount of stars one at a time, handles their animation and sound event.
    private IEnumerator ShowStars(int numberStars, GameObject[] starsUI)
    {
        if(numberStars > 3)
        {
            numberStars = 3;
        }

        for(int i = 0; i < numberStars; i++)
        {
            starsUI[i].GetComponent<Animator>().SetBool("ShowStar", true);
            // Reveal sound event
            startSoundEvent[i].Post(gameObject);
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    // Manage text animation and stat sound event.
    private IEnumerator ShowText(GameObject text)
    {
        text.GetComponent<Animator>().SetBool("ShowText", true);
        statsSoundEvent.Post(gameObject);
        yield return new WaitForSecondsRealtime(0.2f);
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent calls on a disabled/destroyed object
        ScoreManager.Instance.OnShowScoreBoard -= ShowScoreBoard;
    }
}
