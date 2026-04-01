using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIFaceAnimationBehavior : MonoBehaviour
{
    Animator animEyes;
    Animator animMustache;
    Animator animPolterg;
    Animator[] animators;
    Coroutine idleBlink;
    Coroutine rumbling;
    int iBlink;
    int iRumble;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animEyes = this.transform.Find("Eyes").GetComponent<Animator>();
        animMustache = this.transform.Find("Mustache").GetComponent<Animator>();
        animPolterg = this.GetComponentInParent<Animator>();
        animators = new Animator[]{animEyes, animMustache, animPolterg};
        iBlink = -3;
        iRumble = -8;
    }

    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(OnEnableRelated());
        idleBlink = null;
        rumbling = null;
    }

    IEnumerator OnEnableRelated()
    {
        yield return new WaitForFixedUpdate();
        foreach (Animator anim in animators)
        {
            anim.enabled = false;
            anim.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (idleBlink == null) { idleBlink = StartCoroutine(IdleBlink()); }
        if (rumbling == null) { rumbling = StartCoroutine(MustacheRumble()); }
    }

    IEnumerator MustacheRumble()
    {
        float time = 0;

        if (Random.Range(1 + iRumble, 10) > 5)
        {
            time = Random.Range(1f, 3f);
            float speed = 3f / time;
            iRumble = -8;
            animMustache.SetFloat("Speed", speed);
            animMustache.SetBool("IsRumbling", true);
            yield return new WaitForSecondsRealtime(time);
            animMustache.SetBool("IsRumbling", false);
        }
        else
        {
            iRumble++;
        }

        yield return new WaitForSecondsRealtime(5f - time);
        rumbling = null;
    }

    IEnumerator IdleBlink()
    {
        float time = 0;

        if (Random.Range(1 + iBlink, 10) > 5)
        {
            time = Random.Range(.1f, .3f);
            iBlink = -3;
            animEyes.SetBool("IsBlinking", true);
            yield return new WaitForSecondsRealtime(time);
            animEyes.SetBool("IsBlinking", false);
        }
        else
        {
            iBlink++;
        }

        yield return new WaitForSecondsRealtime(5f - time);
        idleBlink = null;
    }
}
