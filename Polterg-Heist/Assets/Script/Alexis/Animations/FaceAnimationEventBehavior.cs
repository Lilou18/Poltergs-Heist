using UnityEngine;
using System.Collections;

public class FaceAnimationEventBehavior : MonoBehaviour
{
    Rigidbody2D playerRbd2D;
    MovementController playerCon;
    RotationManager playerManager;
    Animator animEyes;
    Animator animMustache;
    Coroutine turnBlink;
    Coroutine idleBlink;
    Coroutine rumbling;
    int iBlink;
    int iRumble;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRbd2D = this.GetComponentInParent<Rigidbody2D>();
        playerCon = playerRbd2D.GetComponent<MovementController>();
        playerManager = this.GetComponentInParent<RotationManager>();
        animEyes = this.transform.Find("Eyes").GetComponent<Animator>();
        animMustache = this.transform.Find("Mustache").GetComponent<Animator>();
        iBlink = -3;
        iRumble = -8;
    }

    // Update is called once per frame
    void Update()
    {
        if (playerManager.inRotation) 
        {
            if (turnBlink == null) 
            {
                if (idleBlink != null)
                {
                    StopCoroutine(idleBlink);
                    idleBlink = null;
                }
                turnBlink = StartCoroutine(TurnBlink()); 
            }
        }
        else
        {
            if (turnBlink != null) 
            { 
                StopCoroutine(turnBlink);
                animEyes.SetBool("IsBlinking", false);
                turnBlink = null;
            }
        }
        if (idleBlink == null && turnBlink == null) { idleBlink = StartCoroutine(IdleBlink()); }

        Vector2 moveInput = playerCon.move.ReadValue<Vector2>();
        if (moveInput.x == 0)
        {
            animMustache.SetBool("IsRunning", false);
        }
        else
        {
            if (rumbling != null)
            {
                StopCoroutine(rumbling);
                animMustache.SetBool("IsRumbling", false);
                rumbling = null;
                iRumble = -3;
            }

            animMustache.SetFloat("Direction", Mathf.Round(moveInput.x));
            animMustache.SetBool("IsRunning", true);
        }

        if (moveInput == Vector2.zero)
        {
            if (rumbling == null) { rumbling = StartCoroutine(MustacheRumble()); }
        }
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

    IEnumerator TurnBlink()
    {
        iBlink = -3;
        animEyes.SetBool("IsBlinking", true);
        yield return new WaitForSecondsRealtime(.5f);
        animEyes.SetBool("IsBlinking", false);
        turnBlink = null;
    }
}
