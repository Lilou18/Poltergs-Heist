using System.Collections;
using UnityEngine;

public class ObjectAccessManager : InteractibleManager
{
    [SerializeField] protected AK.Wwise.Event lockedDoorSoundEvent;
    [SerializeField] protected AK.Wwise.Event doorUnlockSoundEvent;
    InventorySystem inventory;
    [SerializeField] KeyItemBehavior keyItemForActivation;
    Collider2D objCollider;

    private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
        inventory = FindFirstObjectByType<InventorySystem>().GetComponent<InventorySystem>();
        objCollider = GetComponent<Collider2D>();
        animator = GetComponentInChildren<Animator>();
        InventorySystem.Instance.OnResetKey += ResetObjectCollider;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (InventorySystem.Instance.IsKeyPickedUp(keyItemForActivation))
        {
            // Animation lock open up
            StartCoroutine(UnlockDoorAnimation());
            //objCollider.isTrigger = true;
            //InventorySystem.Instance.RemoveObject(keyItemForActivation);    // Remove the key from inventory
        }
        else
        {
            lockedDoorSoundEvent.Post(gameObject);
        }
    }

    private void ResetObjectCollider(KeyItemBehavior key)
    {
        if(key == keyItemForActivation)
        {
            objCollider.isTrigger = false;
            animator.SetBool("IsUnlock", false);
        }
    }

    private void OnDisable()
    {
        InventorySystem.Instance.OnResetKey -= ResetObjectCollider;
    }

    private IEnumerator UnlockDoorAnimation()
    {
        animator.SetBool("IsUnlock", true);
        doorUnlockSoundEvent.Post(gameObject);
        yield return new WaitForSeconds(0.5f);
        objCollider.isTrigger = true;
        InventorySystem.Instance.RemoveObject(keyItemForActivation);    // Remove the key from inventory
    }
}
