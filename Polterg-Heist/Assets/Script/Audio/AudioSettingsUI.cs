using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TMP_Text musicNumber;
    [SerializeField] private TMP_Text sfxNumber;
    [SerializeField] protected AK.Wwise.RTPC Volume_Music;
    [SerializeField] protected AK.Wwise.RTPC Volume_SFX;

    private void Start()
    {
        musicSlider.value = AudioManager.Instance.savedMusicVolume;
        sfxSlider.value = AudioManager.Instance.savedSFXVolume;

        musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        musicSlider.onValueChanged.AddListener(UpdateTextMusic);
        sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        sfxSlider.onValueChanged.AddListener(UpdateTextSFX);

        musicNumber.text = Mathf.RoundToInt(musicSlider.value).ToString();
        sfxNumber.text = Mathf.RoundToInt(sfxSlider.value).ToString();

    }

    private void UpdateTextMusic(float volume)
    {
        musicNumber.text = Mathf.RoundToInt(volume).ToString();//volume.ToString();
    }

    private void UpdateTextSFX(float volume)
    {
        sfxNumber.text = Mathf.RoundToInt(volume).ToString();//volume.ToString();
    }

    void OnDestroy()
    {
        // Nettoyer les listeners pour éviter les fuites de mémoire
        musicSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetMusicVolume);
        sfxSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetSFXVolume);
        musicSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetMusicVolume);
        sfxSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetSFXVolume);
    }
}
