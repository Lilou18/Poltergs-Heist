using UnityEngine;
using System.Collections;

public class BreakableObject : SoundDetection, IResetInitialState
{
    [SerializeField] private GameObject prefabBrokenObject;
    [SerializeField] Material greyMaterial;
    private GameObject brokenObject;
    private GameObject hiddenGameObject;
    private SpriteRenderer spriteRenderer;
    private Collider2D objCollider;
    private Rigidbody2D rb;
    private LayerMask floorLayer;

    private Vector2 initialPosition;
    private Sprite initialSprite;

    [SerializeField] protected AK.Wwise.Event breakingSound;
    protected void Awake()
    {

    }
    protected override void Start()
    {
        base.Start();
        objectType = SoundEmittingObject.BreakableObject;
        floorLayer = LayerMask.NameToLayer("Floor");
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        objCollider = gameObject.GetComponent<Collider2D>();
        rb = gameObject.GetComponent<Rigidbody2D>();

        initialPosition = transform.position;
        initialSprite = spriteRenderer.sprite;

        hiddenGameObject = gameObject.transform.GetChild(0).gameObject;
        StartCoroutine(StartRelated());
    }

    IEnumerator StartRelated()
    {
        yield return new WaitForFixedUpdate();
        hiddenGameObject.SetActive(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.layer == floorLayer)
        {
            BreakObject();
        }
    }
    private void BreakObject()
    {
        audioSource.Play();
        breakingSound.Post(gameObject);
        // If there is an hidden object show it
        if (hiddenGameObject != null)
        {
            hiddenGameObject.transform.SetParent(null);
            hiddenGameObject.SetActive(true);
            hiddenGameObject.transform.rotation = Quaternion.identity;
        }
        // Instantiate the broken object
        if (prefabBrokenObject != null)
        {
            brokenObject = Instantiate(prefabBrokenObject, transform.position, Quaternion.identity);   
            if(greyMaterial != null)
            {
                SpriteRenderer spriteBrokenObject = brokenObject.GetComponent<SpriteRenderer>();
                spriteBrokenObject.material = greyMaterial;
            }
        }
        spriteRenderer.sprite = null;
        objCollider.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Static;
        NotifyNearbyEnemies(this);
    }

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
            //hiddenGameObject.GetComponent<PickupItemBehavior>().ResetInitialState();
            hiddenGameObject.GetComponent<IResetInitialState>().ResetInitialState();
            hiddenGameObject.SetActive(false);
        }
        

    }
}
