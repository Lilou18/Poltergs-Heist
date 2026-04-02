using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using System.Linq;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    List<StealableBehavior> stolenItemList = new List<StealableBehavior>();
    List<KeyItemBehavior> keyItemsList = new List<KeyItemBehavior>();

    // Getters
    public List<StealableBehavior> StolenItemList => stolenItemList;
    public event Action<KeyItemBehavior> OnResetKey;

    EnergySystem energy;

    public InputAction Ctrl;
    public InputAction FullEnergy;
    public InputAction MinEnergy;

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

    void Start()
    {
        energy = FindFirstObjectByType<EnergySystem>();

        Ctrl.AddBinding("<Keyboard>/ctrl");
        FullEnergy.AddBinding("<Keyboard>/r");
        MinEnergy.AddBinding("<Keyboard>/e");
        Ctrl.Enable();
        FullEnergy.Enable();
        MinEnergy.Enable();
    }

    private void Update()
    {
        if (Ctrl.IsPressed())
        {
            if (FullEnergy.WasPressedThisFrame())
            {
                Debug.Log("Full energy");
                energy.ModifyEnergy(energy.maxEnergy);
            }
            if (MinEnergy.WasPressedThisFrame())
            {
                Debug.Log("Min energy");
                energy.ModifyEnergy(-energy.maxEnergy);
            }
        }
    }

    // The player stole an item
    public void AddStolenItemToInventory(StealableBehavior stolenItem)
    {
        stolenItemList.Add(stolenItem);
        InventoryUI.Instance.UpdateStealableBarUI(stolenItem, true);
    }

    // The player found a key
    public void AddKeyToInventory(KeyItemBehavior keyItem)
    {
        keyItemsList.Add(keyItem);
        InventoryUI.Instance.UpdateCollectedItemBarUI(keyItem, true);
    }

    // Did the player picked up a particular key item
    public bool IsKeyPickedUp(KeyItemBehavior keyItem)
    {
        if (keyItemsList.Contains(keyItem))
        {
            return true;
        }
        return false;
    }

    // Remove a specefic object from the inventory
    public void RemoveObject(PickupItemBehavior pickupItem)
    {
        // Is the object we want to remove stealable
        if (stolenItemList.Contains(pickupItem))
        {
            stolenItemList.Remove((StealableBehavior)pickupItem);
            InventoryUI.Instance.UpdateStealableBarUI((StealableBehavior)pickupItem, false);
        }
        // Is the object we want to remove a key
        else if (keyItemsList.Contains(pickupItem))
        {
            KeyItemBehavior keyItem = (KeyItemBehavior)pickupItem;
            keyItemsList.Remove(keyItem);
            InventoryUI.Instance.UpdateCollectedItemBarUI(keyItem, false);
            //OnResetKey?.Invoke(keyItem);
        }
    }

    // Reset locked door associated with the key
    public void NotifyKeyReset(KeyItemBehavior keyItem)
    {
        OnResetKey?.Invoke(keyItem);
    }

    
}