using UnityEngine;
using System.Collections;

public class BreakableObject : SoundDetection, IResetInitialState
{
    // Extends SoundDetection for objects that break on floor impact.
    // When broken:
    // - Plays a breaking sound
    // - Reveals a hidden child object (ex: stealable or key)
    // - Spawns a broken version prefab
    // - Disables physics and collisions
    // - Notifies nearby NPCs via SoundDetection

    [SerializeField] private GameObject prefabBrokenObject;     // Prefab spawned at the break position
    [SerializeField] Material greyMaterial;                     // Material applied to the broken prefab to indicate it is not interactable

    [SerializeField] protected AK.Wwise.Event breakingSound;    // Wwise event played when the object breaks

    private GameObject brokenObject;                            // The instantiated broken prefab, destroyed on reset
    private GameObject hiddenGameObject;                        // Child object revealed when the object breaks
    private Vector2 initialPosition;                            // World position at scene start, restored on reset
    private Sprite initialSprite;                               // Original sprite, restored on reset

    private SpriteRenderer spriteRenderer;                      // Cleared when the object breaks
    private Collider2D objCollider;                             // Switched to trigger on break
    private Rigidbody2D rb;                                     // Set to Static on break
    private LayerMask floorLayer;                               // Layer to break the object

    protected void Start()
    {
        objectType = SoundEmittingObject.BreakableObject;
        floorLayer = LayerMask.NameToLayer("Floor");
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        objCollider = gameObject.GetComponent<Collider2D>();
        rb = gameObject.GetComponent<Rigidbody2D>();

        initialPosition = transform.position;
        initialSprite = spriteRenderer.sprite;

        // The hidden object is the first child. It's deactivated until the object breaks
        hiddenGameObject = gameObject.transform.GetChild(0).gameObject;
        StartCoroutine(StartRelated());
    }

    // Waits one physics frame before deactivating the hidden child.
    // Ensures physics has initialized before hiding the object.
    IEnumerator StartRelated()
    {
        yield return new WaitForFixedUpdate();
        hiddenGameObject.SetActive(false);
    }

    // Triggers the break when the object hits the floor.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.layer == floorLayer)
        {
            BreakObject();
        }
    }

    // Handles the full break sequence:
    // 1. Plays sound
    // 2. Reveals hidden child object if present
    // 3. Spawns broken prefab
    // 4. Disables physics and collisions
    // 5. Notifies nearby NPCs
    private void BreakObject()
    {
        breakingSound.Post(gameObject);
        // If there is an hidden object show it
        if (hiddenGameObject != null)
        {
            // Detach the hidden object from breakableObject
            hiddenGameObject.transform.SetParent(null);
            hiddenGameObject.SetActive(true);
            hiddenGameObject.transform.rotation = Quaternion.identity;
        }

        // Spawn the broken version at the same position
        if (prefabBrokenObject != null)
        {
            brokenObject = Instantiate(prefabBrokenObject, transform.position, Quaternion.identity);   
            if(greyMaterial != null)
            {
                SpriteRenderer spriteBrokenObject = brokenObject.GetComponent<SpriteRenderer>();
                spriteBrokenObject.material = greyMaterial;
            }
        }

        // Disable physics and prevent further collisions
        spriteRenderer.sprite = null;
        objCollider.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Static;

        NotifyNearbyEnemies();
    }

    // Restores the object to its initial state.
    // Destroys the broken prefab, reenables physics, and hides the child object.
    public void ResetInitialState()
    {
        Destroy(brokenObject);
        transform.position = initialPosition;
        transform.rotation = Quaternion.identity;
        spriteRenderer.sprite = initialSprite;
        rb.bodyType = RigidbodyType2D.Dynamic;
        objCollider.isTrigger = false;
        if(hiddenGameObject != null)
        {
            hiddenGameObject.transform.SetParent(this.transform);
            hiddenGameObject.transform.localPosition = new Vector3(0, 0, 0);
            hiddenGameObject.GetComponent<IResetInitialState>().ResetInitialState();
            hiddenGameObject.SetActive(false);
        }
        

    }
}
