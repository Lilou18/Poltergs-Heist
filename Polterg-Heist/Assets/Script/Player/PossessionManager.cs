using UnityEngine;
using System.Collections;

/// But: Poss�der des objets
/// Requiert: PossessionController
/// Requiert Externe: PlayerController(1)
/// Input: Click Gauche = possession/d�possession
/// �tat: Ad�quat(temp)
public class PossessionManager : MonoBehaviour, IResetInitialState
{
    public delegate void Callback(string parameter);
    [HideInInspector] public string possessParam = "poss";
    [HideInInspector] public string depossessParam = "deposs";
    public event Callback onPossess;
    public event Callback onDepossess;

    [Header("Sound variables")]
    [SerializeField] public AK.Wwise.Event onPossessionSoundEvent;
    [SerializeField] public AK.Wwise.Event possessionOffSoundEvent;

    //Variables
    [Header("Variables")]
    //[SerializeField] private Sprite normalSprite;
    //[SerializeField] private Sprite possessedSprite;
    //private float duration;            //Vitesse du lerp
    [SerializeField] private float possessionDistance;  // Distance between the player and the object so he can possessed it

    //Conditions
    bool isPossessed;                               //Pour savoir si l'objet possessible est poss�d�
    bool isAnimationFinished;                       //Pour savoir si l'animation de possession est fini
    bool hasEnoughSpace;
    bool hasPosControl;
    public bool isPossessionLocked;
    public bool isAttacked = false;

    //Shortcuts
    PlayerController player;
    PlayerManager manager;
    Animator playerBodyAnim;
    Animator mustacheAnim;
    Transform playerFace;
    IPossessable possession;
    SpriteRenderer spriteRenderer;
    Collider2D col2D;
    PossessionController posControl;

    // Getters
    public bool IsPossessing => isPossessed;
    public PlayerController Player => player;
    public float PossessionDistance => possessionDistance;

    protected void Start()
    {
        //base.Start();
        player = FindFirstObjectByType<PlayerController>();
        playerBodyAnim = player.GetComponentInChildren<HeightAndSpriteResizeSystem>().transform.GetChild(0).Find("Body").GetComponent<Animator>();
        mustacheAnim = playerBodyAnim.transform.parent.Find("Face").Find("Mustache").GetComponent<Animator>();
        manager = FindFirstObjectByType<PlayerManager>();
        playerFace = manager.playerFace;
        spriteRenderer = this.GetComponentInChildren<SpriteRenderer>();
        //if (this.TryGetComponent<SpriteRenderer>(out spriteRenderer)) { }
        //else if (this.transform.GetChild(0).TryGetComponent<SpriteRenderer>(out spriteRenderer)) { }
        //else if (this.transform.GetChild(0).GetChild(0).TryGetComponent<SpriteRenderer>(out spriteRenderer)) { }
        col2D = this.GetComponent<Collider2D>();
        hasPosControl = false;
        if (this.gameObject.TryGetComponent<PossessionController>(out posControl)) { hasPosControl = true; }
        possession = gameObject.GetComponent<IPossessable>();
        //possession.enabled = false;
        isPossessed = false;
        isAnimationFinished = true;
        hasEnoughSpace = true;
        isPossessionLocked = false;
    }

    //Pour l'animation de possession
    IEnumerator AnimationTime()
    {
        if (onPossess != null) { onPossess(possessParam); }
        manager.canRotate = false;
        mustacheAnim.SetBool("IsPossessing", true);

        player.lastPossession = this;
        manager.gameObject.SetActive(true);
        player.GetComponent<Rigidbody2D>().simulated = false;
        gameObject.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        player.GetComponent<Collider2D>().enabled = false;
        player.canMove = false;

        AnimatorClipInfo[] info = playerBodyAnim.GetCurrentAnimatorClipInfo(0);
        float duration = info[0].clip.length;
        isAnimationFinished = false;
        player.isPossessionInProgress = true;

        playerFace.eulerAngles = new Vector3(playerFace.eulerAngles.x, spriteRenderer.transform.eulerAngles.y, playerFace.eulerAngles.z);
        playerFace.SetParent(this.GetComponentInChildren<SpriteRenderer>().transform.Find("FaceTarget"), true);
        playerFace.SetAsLastSibling();
        //Vector3 center = spriteRenderer.transform.localPosition;
        Vector3 center = Vector3.zero;
        center.z = playerFace.position.z;

        float size = col2D.transform.localScale.x * spriteRenderer.transform.localScale.x;
        float width = col2D.bounds.extents.x / size * 2f;
        width = Mathf.Clamp(width, .5f / size, 1.5f / size);
        Vector3 scale = Vector3.one * width;
        manager.FaceLerping(center, scale, duration, false);

        yield return new WaitForSecondsRealtime(duration);
        //yield return new WaitForSecondsRealtime(.5f);
        //manager.gameObject.SetActive(false);
        //yield return new WaitForSecondsRealtime(.5f);
        /*if (possessedSprite != null)
        {
            spriteRenderer.sprite = possessedSprite;
        }*/
        isAnimationFinished = true;
        possession.OnPossessed();
        
        //possession.enabled = true;
        player.isPossessionInProgress = false;
    }

    void FixedUpdate()
    {
        //Pour le mouvement de l'animation
        if (isPossessed)
        {
            Vector3 pos = player.transform.position;
            Vector3 center = col2D.bounds.center;

            if (!isAnimationFinished)
            {
                pos.y = Mathf.Lerp(player.transform.position.y, center.y, 3f * Time.deltaTime);
                pos.x = Mathf.Lerp(player.transform.position.x, center.x, 3f * Time.deltaTime);
            }
            else
            {
                pos.y = center.y;
                pos.x = center.x;
            }

            player.transform.position = pos;
        }
    }

    //Input de possession
    private void OnMouseDown()
    {
        if (Time.timeScale == 0) return;

        if (isPossessionLocked || player.isPossessionInProgress || (posControl != null && posControl.isClimbing) || SuspicionManager.Instance.IsPlayerDead())
        {
            return; // Can't possessed an object if the possession on this object is locked
        }
        if(Vector2.Distance(this.transform.position, player.transform.position) <= possessionDistance)
        {
            if (isAnimationFinished)
            {

                player.GetComponent<Rigidbody2D>().linearVelocityX = 0;

                player.isPossessionInProgress = true;

                //Si le joueur veut poss�der l'objet en poss�dant d�j� un autre
                if (player.isPossessing && !isPossessed)
                {
                    print("PLAYER WANTS TO POSSESSED ANOTHER BOJECT");
                    if (player.lastPossession != null)
                    {
                        player.lastPossession.StopPossession();
                    }

                    isPossessed = true;
                    player.isPossessing = true;
                    onPossessionSoundEvent.Post(gameObject);
                    StartCoroutine(AnimationTime());

                }
                //Si le joueur veut poss�der l'objet
                else if (!player.isPossessing && !isPossessed)
                {
                    isPossessed = true;
                    player.isPossessing = true;
                    onPossessionSoundEvent.Post(gameObject);
                    StartCoroutine(AnimationTime());
                }
                //Si le joueur veut sortir de l'objet
                else if (player.isPossessing && isPossessed && hasEnoughSpace)
                {
                    //print("IM IN THE IF!");
                    StopPossession();
                }
            }
        }
    }

    public void LockPossession(bool locked)
    {
        isPossessionLocked = locked;
        if(posControl != null)
        {
            posControl.IsMoving = false;
        }
    }

    //Pour arr�ter la possession
    public void StopPossession()
    {
        if (posControl != null && posControl.isClimbing && !isAttacked)
        {

            print("NOT DEPOSSESSING CAUSE CLIMBING");
            return;
        }
        print("STOP POSSESSION!");
        if (onDepossess != null) { onDepossess(depossessParam); }

        possessionOffSoundEvent.Post(gameObject);
        isPossessed = false;

        //if(normalSprite != null)
        //    spriteRenderer.sprite = normalSprite;

        possession.OnDepossessed();
        //possession.enabled = false;
        gameObject.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        gameObject.GetComponent<Rigidbody2D>().linearVelocityX = 0;
        player.isPossessing = false;
        player.GetComponent<Collider2D>().enabled = true;
        player.GetComponent<Rigidbody2D>().simulated = true;

        if (hasPosControl)
        {
            if (posControl.isInContact)
                player.transform.position += new Vector3(0, player.GetComponent<Collider2D>().bounds.extents.y - col2D.bounds.extents.y, 0);
        }
        else
            player.transform.position += new Vector3(0, player.GetComponent<Collider2D>().bounds.extents.y - col2D.bounds.extents.y + 0.1f, 0);

        manager.GetComponent<PlayerManager>().VariablesToDefaultValues();
        //manager.gameObject.SetActive(true);
        player.canMove = true;
        player.isPossessionInProgress = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("SizeTrigger"))
        {
            hasEnoughSpace = false;
            //Debug.Log("Entered Trigger");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("SizeTrigger"))
        {
            hasEnoughSpace = true;
            //Debug.Log("Entered Trigger");

            if (isPossessed && hasEnoughSpace)
            {
                // If the player leave a restricted small zone we can now allow him to depossessed the object
                player.isPossessionInProgress = false;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("SizeTrigger"))
        {
            hasEnoughSpace = false;
            //Debug.Log("Entered Trigger");
        }
    }

    public void ResetInitialState()
    {
        if (isPossessed)
        {
            if(posControl != null)
            {
                posControl.isClimbing = false;
            }
            StopPossession();
        }
        isPossessed = false;
        isAnimationFinished = true;
        hasEnoughSpace = true;
        isPossessionLocked = false;
        isAttacked = false;
    }
}
