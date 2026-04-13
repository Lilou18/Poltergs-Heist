using UnityEngine;


public class StealableBehavior : PickupItemBehavior
{
    public AK.Wwise.Event objectPickUpSoundEvent;
    public AK.Wwise.Event playerYaySoundEvent;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnItemPickedUpSound()
    {
        playerYaySoundEvent.Post(gameObject);
        objectPickUpSoundEvent.Post(gameObject);
    }
}
