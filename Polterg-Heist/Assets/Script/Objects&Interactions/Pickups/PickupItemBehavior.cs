using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Collider2D))]
public abstract class PickupItemBehavior : MonoBehaviour, IResetInitialState
{
    // This class manage the behavior of items that can be picked up by the player
    protected SpriteRenderer itemSpriteRenderer;
    protected Collider2D itemCollider;

    protected Light2D lightStealable;
    protected ParticleSystem[] sparkles;
    
    // Getters
    public SpriteRenderer ItemSpriteRenderer {  get { return itemSpriteRenderer; } }

    protected virtual void Awake()
    {
        itemSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        itemCollider = GetComponentInChildren<Collider2D>();
        lightStealable = GetComponentInChildren<Light2D>();
        sparkles = GetComponentsInChildren<ParticleSystem>();
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // We hide the item sprite when the player collect it
        PossessionManager possessionManager = collision.GetComponent<PossessionManager>();
        if (collision.GetComponent<PlayerController>() != null || (possessionManager != null && possessionManager.IsPossessing))//(collision.GetComponent<PossessionController>() != null && collision.GetComponent<KeyController>() == null))
        {
            HideItem();

        }
            
    }

    protected void HideItem()
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

    public virtual void ResetInitialState()
    {
        InventorySystem.Instance.RemoveObject(this);
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
