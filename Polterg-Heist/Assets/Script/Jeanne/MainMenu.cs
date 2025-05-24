using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    //int buttonNb;
    Animator animLogo;
    Animator animButton;
    Animator animPolterg;
    //public float logoDelay;
    //public float buttonDelay;
    //public float poltergDelay;

    bool isEnableFinished;
    Image[] buttons;
    TextMeshProUGUI[] texts;
    Vector4 transButtonColor;
    Vector4 iniTextColor;
    Vector4 transTextColor;
    Coroutine sequence;

    private void Start()
    {
        buttons = this.GetComponentsInChildren<Image>();
        texts = this.GetComponentsInChildren<TextMeshProUGUI>();
        transButtonColor = Vector4.one;
        transButtonColor.w = 0;
        iniTextColor = buttons[0].GetComponentInChildren<TextMeshProUGUI>().color;
        transTextColor = iniTextColor;
        transTextColor.w = 0;

        animButton = this.GetComponent<Animator>();
        animPolterg = this.transform.parent.Find("Polterg").GetComponent<Animator>();
        animLogo = this.transform.parent.Find("Logo").GetComponent<Animator>();
        if (PlayerPrefs.GetInt("FirstTime", 0) == 0)
        {
            PlayerPrefs.SetInt("FirstTime", 1);
            animLogo.SetTrigger("IsFirstStart");
            StartCoroutine(FirstStart(true));
        }
        else
        {
            StartCoroutine(FirstStart(false));
        }
    }

    private void OnEnable()
    {
        print("Enabled");
        isEnableFinished = false;
        Time.timeScale = 1f;
        StartCoroutine(OnEnableRelated());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        foreach (TextMeshProUGUI txt in texts)
        {
            txt.color = transTextColor;
        }
        foreach (Image img in buttons)
        {
            img.color = transButtonColor;
            img.GetComponent<Button>().interactable = false;
        }
        sequence = null;
    }

    IEnumerator OnEnableRelated()
    {
        sequence = null;
        yield return new WaitForFixedUpdate();
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].color = transButtonColor;
            texts[i].color = transTextColor;
        }
        isEnableFinished = true;
    }

    IEnumerator FirstStart(bool first)
    {
        if (first)
        {
            yield return new WaitForSecondsRealtime(.5f);//logoDelay);
            animLogo.SetBool("IsStartDone", true);
            yield return new WaitForSecondsRealtime(2.5f);//buttonDelay);
            animButton.SetBool("IsStartDone", true);
            yield return new WaitForSecondsRealtime(.5f);//poltergDelay);
            animPolterg.SetBool("IsStartDone", true);
        }
        else
        {
            animLogo.SetBool("IsStartDone", true);
            animButton.SetBool("IsStartDone", true);
            animPolterg.SetBool("IsStartDone", true);
        }
    }

    IEnumerator ButtonsApparition()
    {
        yield return new WaitForSecondsRealtime(.5f);

        float animTime = 1f;
        float startTime = Time.time;
        float endTime = startTime + animTime;
        float step = Vector4.Distance(transButtonColor, Vector4.one) / animTime;

        while (Time.time <= endTime)
        {
            float stepTime = step * Time.deltaTime;

            foreach (Image img in buttons)
            {
                img.color = Vector4.MoveTowards(img.color, Vector4.one, stepTime);
            }
            foreach (TextMeshProUGUI txt in texts)
            {
                txt.color = Vector4.MoveTowards(txt.color, iniTextColor, stepTime);
            }

            yield return null;
        }

        foreach (Image img in buttons)
        {
            img.GetComponent<Button>().interactable = true;
        }
    }

    private void Update()
    {
        if (sequence == null && isEnableFinished) { sequence = StartCoroutine(ButtonsApparition()); }
    }

    public void Jouer()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    public void Controls()
    {
        SceneManager.LoadScene("Controls");
    }

    public void Histoire()
    {
        SceneManager.LoadScene("Histoire");
    }

    public void AudioSettings()
    {
        SceneManager.LoadScene("AudioSettings");
    }

    public void Quit()
    {
        PlayerPrefs.SetInt("FirstTime", 0);
        Application.Quit();
    }

    public void Retour()
    {
        SceneManager.LoadScene("UI_Accueil");
    }

    //Load le niveau quon clique
    public void OuvreNiveaux(int niveauId)
    {
        string nomNiveau = "Niveau" + niveauId;
        SceneManager.LoadScene(nomNiveau);
    }
}
