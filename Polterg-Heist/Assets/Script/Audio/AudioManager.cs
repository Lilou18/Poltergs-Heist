using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

public class AudioManager : MonoBehaviour
{
    // Manages background music and audio volume settings across scenes.
    // Persists across scene loads via DontDestroyOnLoad.
    // Maps scenes to music zones and transitions music when the zone changes.
    // Saves and restores volume settings via PlayerPrefs.

    //Singleton
    public static AudioManager Instance { get; private set; }

    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event eventMusic;         // Main music event.
    [SerializeField] private AK.Wwise.RTPC volume_Music;        // Wwise RTPC controlling global music volume      
    [SerializeField] private AK.Wwise.RTPC volume_SFX;          // Wwise RTPC controlling global SFX volume

    // Used to prevent error with typing
    [Header("State Names (Wwise)")]
    [SerializeField] private string menuState = "MUS_Menu";     // Wwise state name for menu music
    [SerializeField] private string houseState = "MUS_House";   // Wwise state name for house level music
    [SerializeField] private string museumState = "MUS_Museum"; // Wwise state name for museum level music

    private float savedMusicVolume;                             // Last saved music volume
    private float savedSFXVolume;                               // Last saved SFX volume
    private MusicZone currentZone = MusicZone.None;             // The music zone currently playing
    private Dictionary<SceneName, MusicZone> sceneMusicDict;    // Maps each scene to its music zone

    // Getters
    public float SavedMusicVolume => savedMusicVolume;
    public float SavedSFXVolume => savedSFXVolume;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeSceneMusicMap();

        // Restore saved volumes or fall back to Wwise defaults
        savedMusicVolume = PlayerPrefs.HasKey("MusicVolume") ? PlayerPrefs.GetFloat("MusicVolume") : volume_Music.GetGlobalValue();
        savedSFXVolume = PlayerPrefs.HasKey("SFXVolume") ? PlayerPrefs.GetFloat("SFXVolume") : volume_SFX.GetGlobalValue();

        SetMusicVolume(savedMusicVolume);
        SetSFXVolume(savedSFXVolume);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Builds the mapping between scene names and their associated music zones.
    private void InitializeSceneMusicMap()
    {
        sceneMusicDict = new Dictionary<SceneName, MusicZone>()
        {
            // Menu music
            { SceneName.MainMenu, MusicZone.Menu },
            { SceneName.LevelSelect, MusicZone.Menu },
            { SceneName.Controls, MusicZone.Menu },
            { SceneName.Story, MusicZone.Menu },
            { SceneName.AudioSettings, MusicZone.Menu },

            // House music
            { SceneName.Niveau1, MusicZone.House },
            { SceneName.Niveau2, MusicZone.House },
            { SceneName.Niveau3, MusicZone.House },

            // Museum music
            { SceneName.Niveau4, MusicZone.Museum },
            { SceneName.Niveau5, MusicZone.Museum },
            { SceneName.Niveau6, MusicZone.Museum }
        };
    }

    // Called automatically when a scene finishes loading.
    // Resolves the music zone for the new scene and transitions music if needed.
    private void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        if (!Enum.TryParse(scene.name, out SceneName sceneEnum))
        {
            Debug.LogError("Scene not in SceneName enum: " + scene.name);
            StopMusic();
            return;
        }

        MusicZone zone = GetMusicZone(sceneEnum);
        HandleMusic(zone);
    }

    // Returns the music zone associated with the given scene.
    // Returns MusicZone.None if the scene has no mapping.
    private MusicZone GetMusicZone(SceneName scene)
    {
        if (sceneMusicDict.TryGetValue(scene, out MusicZone zone))
            return zone;

        return MusicZone.None;
    }

    // Change msuic based on the given zone.
    // Menu music is not restarted if it is already playing.
    private void HandleMusic(MusicZone zone)
    {
        if (zone == currentZone && zone == MusicZone.Menu)
        {
            return;
        }

        currentZone = zone;

        switch (zone)
        {
            case MusicZone.Menu:
                PlayMusicZone(menuState);
                break;

            case MusicZone.House:
                PlayMusicZone(houseState);
                break;

            case MusicZone.Museum:
                PlayMusicZone(museumState);
                break;

            default:
                StopMusic();
                break;
        }
    }

    // Stops the current music event and starts a new one with the given Wwise state.
    private void PlayMusicZone(string state)
    {
        eventMusic.Stop(gameObject);
        eventMusic.Post(gameObject);
        AkUnitySoundEngine.SetState("Music", state);
    }

    // Stops the music event.
    public void StopMusic()
    {
        eventMusic.Stop(gameObject);
    }

    // Sets the global music volume and saves it to PlayerPrefs.
    public void SetMusicVolume(float volume)
    {
        volume_Music.SetGlobalValue(volume);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    // Sets the global SFX volume and saves it to PlayerPrefs.
    public void SetSFXVolume(float volume)
    {
        volume_SFX.SetGlobalValue(volume);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    // Suspends the Wwise audio engine.
    public void PauseAudio()
    {
        AkUnitySoundEngine.Suspend();
    }

    // Stops all currently playing Wwise sounds immediately.
    public void PauseAllAudio()
    {
        AkUnitySoundEngine.StopAll();
    }

    // Resumes the Wwise audio engine after a suspend.
    public void ResumeAudio()
    {
        AkUnitySoundEngine.WakeupFromSuspend();
    }
}

public enum MusicZone
{
    None,   // No music — used for scenes without a mapped zone
    Menu,   // Menu music
    House,  // House level music
    Museum  // Museum level music
}
