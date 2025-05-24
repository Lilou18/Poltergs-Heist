using UnityEngine;

[ExecuteInEditMode]
public class KeyController : PossessionController
{
    [SerializeField] public GameObject targetToUnlock;

    private void OnEnable()
    {
        ComponentChangeSwitch(true);
    }

    private void OnDisable()
    {
        ComponentChangeSwitch(false);
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
        if (collision.gameObject == targetToUnlock)
        {
            collision.gameObject.GetComponent<Collider2D>().enabled = false;
            GetComponent<PossessionManager>().StopPossession();
            transform.position += new Vector3(0, -1000, 0);
            ComponentChangeSwitch(false);
        }
    }

    void ComponentChangeSwitch(bool state)
    {
        gameObject.GetComponent<Collider2D>().isTrigger = !state;
        gameObject.GetComponent<PossessionManager>().enabled = state;
        if (state)
        {
            gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            int layerID = LayerMask.NameToLayer("PossessedObject");
            gameObject.layer = layerID;
            gameObject.tag = "Possess";
        }
        else
        {
            gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            gameObject.layer = 0;
            gameObject.tag = "Untagged";
        }
    }
}
