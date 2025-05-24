using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    [Header ("Sound variables")]
    [SerializeField] protected AK.Wwise.Event fogRemoveSoundEvent;

    Collider2D boxCollider;
    Animator animator;

    private void Start()
    {
        boxCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Possess"))
        {
            PossessionManager possessionManager = collision.GetComponent<PossessionManager>();
            if (possessionManager != null && possessionManager.IsPossessing)
            {
                animator.SetBool("ClearFog", true);
                boxCollider.enabled = false;
                fogRemoveSoundEvent.Post(gameObject);
            }
        }       
        else if(collision.gameObject.CompareTag("Player"))
        {
            animator.SetBool("ClearFog",true);
            boxCollider.enabled = false;
            fogRemoveSoundEvent.Post(gameObject);
        }
    }
}
