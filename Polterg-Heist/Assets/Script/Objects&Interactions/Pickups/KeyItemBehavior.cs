using System;
using System.Collections;
//using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class KeyItemBehavior : PickupItemBehavior, IResetInitialState
{
    // Extends PickupItemBehavior for key items that unlock a linked door.
    // Unlocks the door on pickup and re locks it on reset.

    [SerializeField] private LockedObject linkedDoor;   // The door unlocked when this key is picked up

    // Hides the key and unlocks the linked door.
    protected override void OnPickedUp()
    {
        base.OnPickedUp();
        if(linkedDoor != null)
        {
            linkedDoor.Unlock();
        }        
    }

    // Restores the key's visual state and re locks the linked door.
    public override void ResetInitialState()
    {
        base.ResetInitialState();
        if(linkedDoor != null)
        {
            linkedDoor.Lock();
        }
    }
}
