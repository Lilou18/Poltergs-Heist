using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    // Handles the removal of fog of war when the player or a possessed object enters the trigger.
    // - Plays a sound
    // - Triggers a visual animation
    // - Disables the collider to prevent re-triggering

    [Header ("Sound variables")]
    [SerializeField] protected AK.Wwise.Event fogRemoveSoundEvent;  // Sound played when fog is cleared

    Collider2D fogCollider;                                         // Trigger collider used to detect entry
    Animator animator;                                              // Animator controlling fog visual transition

    private void Start()
    {
        fogCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    // If the player or an object possessed by the player touch the fog of war
    // then we remove it.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Possess"))
        {
            PossessionManager possessionManager = collision.GetComponent<PossessionManager>();
            if (possessionManager != null && possessionManager.IsPossessing)
            {
                ClearFog();
            }
        }       
        else if(collision.gameObject.CompareTag("Player"))
        {
            ClearFog();
        }
    }

    // Remove the Fog of War with an animation and play the sound event.
    private void ClearFog()
    {
        animator.SetBool("ClearFog", true);
        fogCollider.enabled = false;
        fogRemoveSoundEvent.Post(gameObject);
    }
}
