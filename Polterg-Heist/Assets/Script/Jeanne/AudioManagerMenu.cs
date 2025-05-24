using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

public class AudioManagerMenu : MonoBehaviour
{
    public static AudioManagerMenu Instance;

    [SerializeField] public AK.Wwise.Event eventMusic;
    //[SerializeField] public AK.Wwise.Event houseMusic;
    private List<string> menuScenes = new List<string> { "UI_Accueil", "Histoire", "LevelSelect", "Controls" };
    private List<string> houseScenes = new List<string> { "Niveau1", "Niveau2", "Niveau3" };
    private List<string> museumScenes = new List<string> { "Niveau4", "Niveau5", "Niveau6" };
    //public AK.Wwise.State state;
    private bool isMusicPlaying = false;
    

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        //state = GetComponent<AK.Wwise.State>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }



    private void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        print("YEAH!");
        if(IsMenuScene(scene.name) && !isMusicPlaying)
        {
            //AkUnitySoundEngine.SetState("MUS_Start", "MUS_Menu");
            print("MUSIC PLAYING!");
            eventMusic.Post(gameObject);
            AkUnitySoundEngine.SetState("Music", "MUS_Menu");
            isMusicPlaying = true;
        }
        else if (IsHouseScene(scene.name))
        {
            if (isMusicPlaying)
            {
                
                isMusicPlaying = false;
            }
            StopMenuMusic();
            print("HOUSE MUSIC!");
            eventMusic.Post(gameObject);
            AkUnitySoundEngine.SetState("Music", "MUS_House");
            //state.SetValue("MUS_House");
            //AkUnitySoundEngine.SetState("Music / ", "MUS_House");
            //AkUnitySoundEngine.SetSwitch("MUS_Start", "MUS_House", gameObject);
            //eventMusic.Post(gameObject);
            //houseMusic.Post(gameObject);
        }
        else if (IsMuseumScene(scene.name))
        {
            if (isMusicPlaying)
            {

                isMusicPlaying = false;
            }
            StopMenuMusic();
            print("Museum MUSIC!");
            eventMusic.Post(gameObject);
            AkUnitySoundEngine.SetState("Music", "MUS_Museum");
        }
        else if(!IsMenuScene(scene.name) && !IsHouseScene(scene.name) && !IsMuseumScene(scene.name))
        {
            StopMenuMusic();
        }
        
    }

    private bool IsMenuScene(string sceneName)
    {
        if (menuScenes.Contains(sceneName))
        {
            return true;
        }
        return false;
    }

    private bool IsHouseScene(string sceneName)
    {
        if (houseScenes.Contains(sceneName))
        {
            return true;
        }
        return false;
    }
    private bool IsMuseumScene(string sceneName)
    {
        if (museumScenes.Contains(sceneName))
        {
            return true;
        }
        return false;
    }

    public void StopMenuMusic()
    {
        eventMusic.Stop(gameObject);
        isMusicPlaying = false;
    }
}
