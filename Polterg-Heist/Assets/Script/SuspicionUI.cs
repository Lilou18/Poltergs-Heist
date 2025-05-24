using UnityEngine;
using UnityEngine.UI;

public class SuspicionUI : MonoBehaviour
{
    // This class manage the suspicion UI.

    [SerializeField] private Slider suspicionSlider;
    //[SerializeField] public AK.Wwise.Event suspicionBar;
    [SerializeField] protected AK.Wwise.RTPC suspicionBarRTCP;

    private void Start()
    {
        suspicionSlider.value = 0;
        SuspicionManager.Instance.OnSuspicionChanged += UpdateUI;
        //suspicionBar.Post(gameObject);
    }

    private void UpdateUI(float suspiciousAmount)
    {
        //print("UI CHANGE");
        suspicionSlider.value = suspiciousAmount;
        suspicionBarRTCP.SetValue(null, suspiciousAmount * 100);
        //print(suspiciousAmount * 100);
    }

    private void OnDisable()
    {
        SuspicionManager.Instance.OnSuspicionChanged -= UpdateUI;
    }
}
