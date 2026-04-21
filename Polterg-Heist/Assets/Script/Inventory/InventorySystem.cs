using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using System.Linq;

public class InventorySystem : MonoBehaviour
{
    // Manages the player's inventory of stolen and key items.
    // Fires OnInventoryChanged to notify InventoryUI of additions and removals.
    // Removes items when they fire their OnReset event.


    // Singleton
    public static InventorySystem Instance { get; private set; }

    [SerializeField] private InventoryUI inventoryUI;

    List<PickupItemBehavior> stolenItemList = new List<PickupItemBehavior>();   // Items the player has stolen
    List<PickupItemBehavior> keyItemsList = new List<PickupItemBehavior>();     // Key items the player has collected

    // Fired when an item is added or removed. bool is true on pickup, false on removal
    public event Action<PickupItemBehavior, bool> OnInventoryChanged;

    // Getter
    public IReadOnlyList<PickupItemBehavior> StolenItemList => stolenItemList;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Adds the item to the correct inventory list based on its type.
    // Subscribes to OnReset so the item is removed automatically when reset.
    public void AddItem(PickupItemBehavior item)
    {
        switch (item.ItemType)
        {
            case PickupItemType.Stealable:
                stolenItemList.Add(item);
                break;
            case PickupItemType.Key:
                keyItemsList.Add(item);
                break;
        }
        item.OnReset += HandleItemReset;
        OnInventoryChanged?.Invoke(item, true);
    }

    // Removes the item from the correct inventory list and unsubscribes from OnReset.
    public void RemoveItem(PickupItemBehavior item)
    {
        switch (item.ItemType)
        {
            case PickupItemType.Key:
                keyItemsList.Remove(item);
                break;

            case PickupItemType.Stealable:
                stolenItemList.Remove(item);
                break;
        }
        item.OnReset -= HandleItemReset;
        OnInventoryChanged?.Invoke(item, false);
    }

    // Called when a tracked item fires its OnReset event.
    // Removes the item from inventory automatically.
    private void HandleItemReset(PickupItemBehavior item)
    {
        RemoveItem(item);
    }
}