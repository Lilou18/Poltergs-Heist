using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Sound variables")]
    [SerializeField] protected AK.Wwise.Event mouseHoverSoundEvent;
    [SerializeField] protected AK.Wwise.Event onClickSoundEvent;

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (button.interactable)
        {
            onClickSoundEvent.Post(gameObject);
        }

    }

    // This class manage the sound made by the buttons when we hover and click on them

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
        {
            mouseHoverSoundEvent.Post(gameObject);
        }

    }
}
