using UnityEngine;
using System.Collections;

public abstract class SpriteManager : MonoBehaviour
{
    [SerializeField] public AK.Wwise.Event moveFailSoundEvent;
    bool soundIsPlaying = false;

    protected Vector2 lastPos;
    protected Vector2 input;
    protected bool goesLeft;
    protected bool goesRight;
    bool hasPlayerCon;
    bool hasPosCon;

    //Shortcut
    protected PlayerController player;
    protected PossessionController posCon;

    protected virtual void Awake()
    {
        goesLeft = false;
        goesRight = false;
        hasPlayerCon = false;
        hasPosCon = false;
        if (this.transform.parent.TryGetComponent<PlayerController>(out player)) { hasPlayerCon = true; }
        else if (this.transform.parent.parent.TryGetComponent<PlayerController>(out player)) { hasPlayerCon = true; }
        else if (this.transform.parent.parent.parent.TryGetComponent<PlayerController>(out player)) { hasPlayerCon = true; }
        else if (this.transform.parent.TryGetComponent<PossessionController>(out posCon)) { hasPosCon = true; }
        else if (this.transform.parent.parent.TryGetComponent<PossessionController>(out posCon)) { hasPosCon = true; }
        else if (this.transform.parent.parent.parent.TryGetComponent<PossessionController>(out posCon)) { hasPosCon = true; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {

    }

    // Update is called once per frame
    protected virtual void Update()
    {
        float posDiff = lastPos.x - transform.position.x;

        if (input.x < 0)
        {
            goesRight = false;
            goesLeft = true;
        }
        else if (input.x > 0)
        {
            goesLeft = false;
            goesRight = true;
        }

        if (hasPlayerCon)
            input = player.move.ReadValue<Vector2>();
        else if (hasPosCon)
            input = posCon.move.ReadValue<Vector2>();
        else
            input = new Vector2(-posDiff, 0);
    }

    IEnumerator ResetBoolAnimation(Animator anim, string param)
    {
        yield return new WaitForSecondsRealtime(.01f);
        anim.SetBool(param, false);
    }

    protected void CollisionConditionsForManager(string param, MovementController controller, Animator anim)
    {
        if (param == controller.landParam)
        {
            float temp = (controller.lastPosY - controller.transform.position.y) / 10f;
            anim.SetFloat("velocityY", temp);
            anim.SetTrigger("squashLand");
        }
        else if (param == controller.jumpParam)
        {
            anim.SetTrigger("stretch");
        }
        else if (param == controller.bonkRightParam)
        {
            anim.SetTrigger("squashRight");
        }
        else if (param == controller.bonkLeftParam)
        {
            anim.SetTrigger("squashLeft");
        }
        else if (param == controller.cantWalkLeftParam)
        {
            anim.SetFloat("velocityX", -1f);
            anim.SetTrigger("moveFail");
            moveFailSoundEvent.Post(gameObject);
            //StartCoroutine(MoveFailSoundEvent());
        }
        else if (param == controller.cantWalkRightParam)
        {
            anim.SetFloat("velocityX", 1f);
            anim.SetTrigger("moveFail");
            moveFailSoundEvent.Post(gameObject);
            //StartCoroutine(MoveFailSoundEvent());
        }
    }

    //private IEnumerator MoveFailSoundEvent()
    //{
    //    if (!soundIsPlaying)
    //    {
    //        soundIsPlaying = true;
    //        yield return new WaitForSeconds(2f);
    //        moveFailSoundEvent.Post(gameObject);
    //        soundIsPlaying = false;
            
    //    }

    //}
}
