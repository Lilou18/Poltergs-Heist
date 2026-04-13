using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using System.Linq;

public class InventorySystem : MonoBehaviour
{
    // Manages the player's inventory of stolen items and key items.
    // Notifies InventoryUI on any change.
    // Also exposes the OnResetKey event used to notify locked doors when a key is reset.


    public static InventorySystem Instance { get; private set; }            // Singleton

    List<StealableBehavior> stolenItemList = new List<StealableBehavior>(); // Items the player has stolen
    List<KeyItemBehavior> keyItemsList = new List<KeyItemBehavior>();       // Key items the player has collected

    // Getters
    public IReadOnlyList<StealableBehavior> StolenItemList => stolenItemList;

    // Fired when a key item is reset — used by locked doors to re-lock themselves
    public event Action<KeyItemBehavior> OnResetKey;

    // Fired when the UI of a StealableBarUI must change
    public event Action<StealableBehavior, bool> OnStealableChanged;
    // Fired when the UI of a CollectedItemBarUI must change
    public event Action<KeyItemBehavior, bool> OnKeyItemChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Adds a stolen item to the inventory and updates the UI.
    public void AddStolenItemToInventory(StealableBehavior stolenItem)
    {
        stolenItemList.Add(stolenItem);
        OnStealableChanged?.Invoke(stolenItem, true);
    }

    // Adds a key item to the inventory and updates the UI.
    public void AddKeyToInventory(KeyItemBehavior keyItem)
    {
        keyItemsList.Add(keyItem);
        OnKeyItemChanged?.Invoke(keyItem, true);
    }

    // Returns true if the given key item is currently in the inventory.
    public bool IsKeyPickedUp(KeyItemBehavior keyItem)
    {
        return keyItemsList.Contains(keyItem);
    }

    // Removes an item from the inventory and updates the UI.
    // Handles both stealable items and key items.
    public void RemoveObject(PickupItemBehavior pickupItem)
    {
        // Is the object we want to remove stealable
        if (stolenItemList.Contains(pickupItem))
        {
            stolenItemList.Remove((StealableBehavior)pickupItem);
            OnStealableChanged?.Invoke((StealableBehavior)pickupItem, false);
        }
        // Is the object we want to remove a key
        else if (keyItemsList.Contains(pickupItem))
        {
            KeyItemBehavior keyItem = (KeyItemBehavior)pickupItem;
            keyItemsList.Remove(keyItem);
            OnKeyItemChanged?.Invoke(keyItem, false);
        }
    }

    // Fires the OnResetKey event for the given key item.
    // Called when a key is reset so locked doors can relock themselves.
    public void NotifyKeyReset(KeyItemBehavior keyItem)
    {
        OnResetKey?.Invoke(keyItem);
    }

    
}