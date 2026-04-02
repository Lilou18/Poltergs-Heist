using System;
using System.Collections;
//using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class KeyItemBehavior : PickupItemBehavior, IResetInitialState
{
    // This class manage the behavior of the key that can be collected by the player

    [SerializeField] private AK.Wwise.Event keyPickUpSound;

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        PossessionManager possessionManager = collision.GetComponent<PossessionManager>();
        if (collision.GetComponent<PlayerController>() != null || (possessionManager != null && possessionManager.IsPossessing))
        {
            InventorySystem.Instance.AddKeyToInventory(this);
            keyPickUpSound.Post(gameObject);
        }
    }

    // Reset key item to it's inital state
    public override void ResetInitialState()
    {
        base.ResetInitialState();
        InventorySystem.Instance.NotifyKeyReset(this);
    }

}
