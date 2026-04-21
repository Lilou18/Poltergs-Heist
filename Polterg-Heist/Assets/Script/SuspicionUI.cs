using UnityEngine;
using UnityEngine.UI;

public class SuspicionUI : MonoBehaviour
{
    // Display the current suspicion level via a slider.

    [SerializeField] private Slider suspicionSlider;            // Slider representing the suspicion bar
    [SerializeField] protected AK.Wwise.RTPC suspicionBarRTCP;  // Distortion of the background music when
                                                                // suspicion rises.

    private void Start()
    {
        suspicionSlider.value = 0;
        SuspicionManager.Instance.OnSuspicionChanged += UpdateUI;
    }

    // Updates the suspicion slider and the Wwise RTPC with the new suspicion value.
    private void UpdateUI(float suspiciousAmount)
    {
        suspicionSlider.value = suspiciousAmount;
        suspicionBarRTCP.SetValue(null, suspiciousAmount * 100);
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent calls on a disabled/destroyed object
        SuspicionManager.Instance.OnSuspicionChanged -= UpdateUI;
    }
}
