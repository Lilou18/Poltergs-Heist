using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum PickupItemType
{
    Key,        // Key item — unlocks a linked door when picked up
    Stealable   // Treasure item
}

[RequireComponent(typeof(Collider2D))]
public class PickupItemBehavior : MonoBehaviour, IResetInitialState
{
    // Base class for all items that can be picked up by the player.
    // Handles:
    // - Trigger detection for player and possessed objects
    // - Adding the item to the inventory via InventorySystem
    // - Hiding the item on pickup (sprite, collider, light, particles)
    // - Playing pickup sounds
    // - Restoring the item on reset

    [SerializeField] private AK.Wwise.Event[] PickUpSoundsEvents;                   // Sounds played when the item is picked up
    [SerializeField] protected PickupItemType itemType = PickupItemType.Stealable;  // Type of item — determines inventory list and behavior

    protected SpriteRenderer itemSpriteRenderer;                                    // Item sprite, hidden on pickup
    protected Collider2D itemCollider;                                              // Item collider, disabled on pickup
    protected Light2D lightStealable;                                               // Optional light on the item, disabled on pickup
    protected ParticleSystem[] sparkles;                                            // Optional particle effects, deactivated on pickup

    public event Action<PickupItemBehavior> OnReset;                                // Fired when the item is reset to its initial state

    // Getters
    public SpriteRenderer ItemSpriteRenderer {  get { return itemSpriteRenderer; } }

    public PickupItemType ItemType => itemType;

    protected virtual void Awake()
    {
        itemSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        itemCollider = GetComponentInChildren<Collider2D>();
        lightStealable = GetComponentInChildren<Light2D>();
        sparkles = GetComponentsInChildren<ParticleSystem>();
    }

    // Triggered when a collider enters the item's trigger zone.
    // Pickup is registered if the collider belongs to the player directly
    // or to a possessed object currently controlled by the player.
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // We hide the item sprite when the player collect it
        PossessionManager possessionManager = collision.GetComponent<PossessionManager>();
        if (collision.GetComponent<PlayerController>() != null || (possessionManager != null && possessionManager.IsPossessing))
        {
            InventorySystem.Instance.AddItem(this);
            OnPickedUp();
            PlayPickupSounds();
        }            
    }

    // Hides all visual and physical components of the item when picked up.
    protected virtual void OnPickedUp()
    {
        itemSpriteRenderer.enabled = false;
        itemCollider.enabled = false;
        if(lightStealable != null)
        {
            lightStealable.enabled = false;
        }
        if(sparkles != null)
        {
            foreach (ParticleSystem particle in sparkles)
            {
                particle.gameObject.SetActive(false);
            }
        }
    }

    // Plays all configured pickup sound events.
    private void PlayPickupSounds()
    {
        if (PickUpSoundsEvents != null)
        {
            foreach(AK.Wwise.Event sound in PickUpSoundsEvents)
            {
                sound.Post(gameObject);
            }
        }
    }

    // Restores all visual and physical components.
    // InventorySystem listens to OnReset to remove the item from the inventory.
    public virtual void ResetInitialState()
    {
        OnReset?.Invoke(this);
        itemSpriteRenderer.enabled = true;
        itemCollider.enabled = true;
        if (lightStealable != null)
        {
            lightStealable.enabled = true;
        }
        if (sparkles != null)
        {
            foreach (ParticleSystem particle in sparkles)
            {
                particle.gameObject.SetActive(true);
            }
        }
    }
}
