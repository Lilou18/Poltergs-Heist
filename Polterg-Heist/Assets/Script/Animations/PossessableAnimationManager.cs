using UnityEngine;

public class PossessableAnimationManager : SpriteManager
{
    Animator animPos;

    protected override void Awake()
    {
        base.Awake();
        animPos = this.transform.parent.GetComponent<Animator>();
    }

    private void OnEnable()
    {
        posCon.onJump += EventParameter;
        posCon.onLand += EventParameter;
        posCon.onBonkL += EventParameter;
        posCon.onBonkR += EventParameter;
        posCon.onShakeL += EventParameter;
        posCon.onShakeR += EventParameter;
    }

    private void OnDisable()
    {
        posCon.onJump -= EventParameter;
        posCon.onLand -= EventParameter;
        posCon.onBonkL -= EventParameter;
        posCon.onBonkR -= EventParameter;
        posCon.onShakeL -= EventParameter;
        posCon.onShakeR -= EventParameter;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        animPos.SetBool("canJump", posCon.canJump);
        animPos.SetBool("canWalk", posCon.canWalk);
        animPos.SetFloat("animSpeed", (int)posCon.weightOfAnimation / 2f);
    }

    void EventParameter(string param)
    {
        CollisionConditionsForManager(param, posCon, animPos);
    }
}
