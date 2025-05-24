using UnityEngine;
using System.Collections;

public class PlayerManager : RotationManager, IResetInitialState
{
    //General animation variables
    Animator baseHeight;
    Animator bodyAnim;
    Animator mustacheAnim;
    [HideInInspector] public Transform playerFace;
    SpriteRenderer[] faceSpriteArray;
    PossessionManager lastPossession;
    //Coroutine lerpFace;
    private Vector3 facePosIni;
    private Vector3 faceScaleIni;
    private Quaternion faceRotIni;

    private Vector3 scaleIni;

    private bool isFaceLerpFinished;

    public Vector3 FacePositionIni { get { return facePosIni; } }
    public Vector3 FaceScaleIni { get { return faceScaleIni; } }
    public Quaternion FaceRotationIni { get { return faceRotIni; } }

    protected override void Start()
    {
        base.Start();
        scaleIni = this.transform.lossyScale;
        lastPossession = null;
        faceSpriteArray = playerFace.GetComponentsInChildren<SpriteRenderer>(true);
    }

    void OnEnable()
    {
        baseHeight = GetComponentInParent<HeightAndSpriteResizeSystem>().GetComponent<Animator>();
        bodyAnim = baseHeight.transform.GetChild(0).Find("Body").GetComponent<Animator>();
        mustacheAnim = bodyAnim.transform.parent.Find("Face").Find("Mustache").GetComponent<Animator>();
        playerFace = bodyAnim.transform.parent.Find("Face");
        facePosIni = playerFace.localPosition;
        faceScaleIni = playerFace.localScale;
        faceRotIni = playerFace.rotation;
        player.onJump += EventParameter;
        player.onLand += EventParameter;
        player.onBonkR += EventParameter;
        player.onBonkL += EventParameter;
        isFaceLerpFinished = false;
        StartCoroutine(OnEnableRelated());
    }

    void OnDisable()
    {
        player.onJump -= EventParameter;
        player.onLand -= EventParameter;
        player.onBonkR -= EventParameter;
        player.onBonkL -= EventParameter;
        if (lastPossession != null) 
        {
            lastPossession.onDepossess -= PossessionParameter;
            lastPossession.onDepossess -= PossessionParameter; 
        }
    }

    protected override void Update()
    {
        base.Update();
        baseHeight.SetBool("inContact", player.isInContact);
        if (lastPossession != player.lastPossession)
        {
            //bodyAnim.SetTrigger("IsDepossessing");
            bodyAnim.SetTrigger("IsPossessing");
            if (lastPossession != null) 
            {
                lastPossession.onPossess -= PossessionParameter;
                lastPossession.onDepossess -= PossessionParameter; 
            }
            lastPossession = player.lastPossession;
            lastPossession.onPossess += PossessionParameter;
            lastPossession.onDepossess += PossessionParameter;
        }
        if (isFaceLerpFinished && playerFace.parent.GetComponentInParent<SpriteRenderer>())
        {
            SpriteRenderer parentSprite = playerFace.parent.GetComponentInParent<SpriteRenderer>();

            foreach (SpriteRenderer sprite in faceSpriteArray) 
            { 
                sprite.sortingLayerName = parentSprite.sortingLayerName; 
            }
        }
    }

    IEnumerator OnEnableRelated()
    {
        yield return new WaitForEndOfFrame();
        isFaceLerpFinished = true;
        canRotate = true;
    }

    IEnumerator DepossessionDelay()
    {
        yield return new WaitForFixedUpdate();
        //playerFace.SetParent(player.transform, true);
        playerFace.SetParent(bodyAnim.transform.parent, true);
        playerFace.SetAsLastSibling();
        //playerFace.eulerAngles = new Vector3(playerFace.eulerAngles.x, player.transform.eulerAngles.y, playerFace.eulerAngles.z);
        AnimatorClipInfo[] clip = bodyAnim.GetCurrentAnimatorClipInfo(0);
        FaceLerping(facePosIni, faceScaleIni, clip[0].clip.length, true);
    }

    IEnumerator FaceLerp(Vector3 posTarget, Vector3 scalTarget, float duration, bool isDepossession)
    {
        isFaceLerpFinished = false;
        foreach (SpriteRenderer sprite in faceSpriteArray)
        {
            if (sprite.transform.name == "Pupil") { sprite.sortingOrder = 12; }
            else { sprite.sortingOrder = 11; }
            sprite.sortingLayerName = "Player";
        }
        //for (int i = 0; i < faceSpriteArray.Length; i++) { faceSpriteArray[i].sortingLayerName = "Player"; }

        float endTime = Time.time + duration;
        float angle = Quaternion.Angle(playerFace.rotation, faceRotIni) / duration;
        float distance = Vector3.Distance(playerFace.localPosition, posTarget) / duration;
        float sizeDiff = Vector3.Distance(playerFace.localScale, scalTarget) / duration;

        while (Time.time <= endTime)
        {
            float deltaTime = Time.deltaTime;
            playerFace.rotation = Quaternion.RotateTowards(playerFace.rotation, faceRotIni, angle * deltaTime);
            playerFace.localPosition = Vector3.MoveTowards(playerFace.localPosition, posTarget, distance * deltaTime);
            playerFace.localScale = Vector3.MoveTowards(playerFace.localScale, scalTarget, sizeDiff * deltaTime);
            yield return null;
        }
        playerFace.rotation = faceRotIni;
        playerFace.localPosition = posTarget;
        playerFace.localScale = scalTarget;

        if (isDepossession) 
        {
            mustacheAnim.SetBool("IsPossessing", false);
            canRotate = true;
        }
        else
        {
            foreach (SpriteRenderer sprite in faceSpriteArray)
            {
                SpriteRenderer parentSprite = playerFace.parent.GetComponentInParent<SpriteRenderer>();
                int parentOrder = parentSprite.sortingOrder;

                if (sprite.transform.name == "Pupil") { sprite.sortingOrder = parentOrder + 2; }
                else { sprite.sortingOrder = parentOrder + 1; }

                sprite.sortingLayerName = parentSprite.sortingLayerName;
            }
            //for (int i = 0; i < faceSpriteArray.Length; i++) { faceSpriteArray[i].sortingLayerName = playerFace.parent.GetComponentInParent<SpriteRenderer>().sortingLayerName; }
        }
        isFaceLerpFinished = true;
    }

    public void PossessionParameter(string param)
    {
        if (param == lastPossession.possessParam)
        {
            //Debug.Log("Possessing Animation");
            bodyAnim.SetTrigger("IsPossessing");
        }
        else if (param == lastPossession.depossessParam)
        {
            //Debug.Log("Depossessing Animation");
            bodyAnim.SetTrigger("IsDepossessing");
            StartCoroutine(DepossessionDelay());
        }
    }

    public void FaceLerping(Vector3 positionTarget, Vector3 scaleTarget, float time, bool isDepossession)
    {
        StopAllCoroutines();
        StartCoroutine(FaceLerp(positionTarget, scaleTarget, time, isDepossession));
    }

    public void EventParameter(string param)
    {
        CollisionConditionsForManager(param, player, baseHeight);
    }

    public void VariablesToDefaultValues()
    {
        this.transform.localScale = scaleIni;
    }

    public void ResetInitialState()
    {
        StopAllCoroutines();
        isFaceLerpFinished = true;
        playerFace.SetParent(bodyAnim.transform.parent, true);
        playerFace.SetAsLastSibling();
        for (int i = 0; i < faceSpriteArray.Length; i++) { faceSpriteArray[i].sortingLayerName = "Player"; }
        playerFace.localPosition = facePosIni;
        playerFace.rotation = faceRotIni;
        playerFace.localScale = faceScaleIni;
    }
}
