using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    // Displays and controls audio volume sliders for music and SFX.
    // Reads initial values from AudioManager and writes changes back via its public API.
    // Cleans up slider listeners on destroy to prevent stale callbacks.

    [SerializeField] private Slider sfxSlider;      // Slider controlling SFX volume
    [SerializeField] private Slider musicSlider;    // Slider controlling music volume
    [SerializeField] private TMP_Text musicNumber;  // Displays the current music volume
    [SerializeField] private TMP_Text sfxNumber;    // Displays the current SFX volume

    private void Start()
    {
        // Initialize sliders with saved values from AudioManager
        musicSlider.value = AudioManager.Instance.SavedMusicVolume;
        sfxSlider.value = AudioManager.Instance.SavedSFXVolume;

        // Register listeners. Each slider updates both the audio engine and the display text
        musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        musicSlider.onValueChanged.AddListener(UpdateTextMusic);
        sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        sfxSlider.onValueChanged.AddListener(UpdateTextSFX);

        // Initialize display text to match the current slider values
        musicNumber.text = Mathf.RoundToInt(musicSlider.value).ToString();
        sfxNumber.text = Mathf.RoundToInt(sfxSlider.value).ToString();

    }

    // Updates the music volume label when the slider value changes.
    private void UpdateTextMusic(float volume)
    {
        musicNumber.text = Mathf.RoundToInt(volume).ToString();
    }

    // Updates the SFX volume label when the slider value changes.
    private void UpdateTextSFX(float volume)
    {
        sfxNumber.text = Mathf.RoundToInt(volume).ToString();
    }

    // Unregisters all slider listeners to prevent callbacks after this object is destroyed.
    void OnDestroy()
    {
        musicSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetMusicVolume);
        musicSlider.onValueChanged.RemoveListener(UpdateTextMusic);

        sfxSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetSFXVolume);
        sfxSlider.onValueChanged.RemoveListener(UpdateTextSFX);
    }
}
