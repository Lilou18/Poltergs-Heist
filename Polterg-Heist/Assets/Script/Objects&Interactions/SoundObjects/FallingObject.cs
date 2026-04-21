using UnityEngine;

public class FallingObject : SoundDetection, IPossessable, IResetObject, IResetInitialState
{
    // Extends SoundDetection for objects that fall when possessed.
    // When possessed, releases physics so the object falls under gravity.
    // On floor impact, plays a sound and notifies nearby NPCs.
    // Can be reset to its initial position via ResetObject.

    [SerializeField] public AK.Wwise.Event fallingObject;   // Wwise sound event played when the object hit the floor

    protected Vector2 initialPosition;                      // World position at scene start, restored on reset
    private Rigidbody2D rb;                                 // Switched to Dynamic on possession to enable falling
    private Collider2D objectCollider;


    protected void Start()
    {
        initialPosition = transform.position;
        rb= GetComponent<Rigidbody2D>();
        objectCollider= GetComponent<Collider2D>();
        objectType = SoundEmittingObject.FallingObject;
    }

    // Releases the object from kinematic physics so it falls under gravity.
    public void OnPossessed()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
    }
    public void OnDepossessed()
    {
        // No behavior on depossession — object continues falling until it hits the floor
    }

    // Plays a sound and notifies nearby NPCs when the object hits the floor.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // When the object collide with the floor it makes a sound that alert nearby enemies.
        if (collision.gameObject.CompareTag("Floor"))
        {
            fallingObject.Post(gameObject);
            NotifyNearbyEnemies();
        }
    }

    // Stops physics and moves the object back to its initial position and rotation.
    // Called by NPCInvestigationController after investigation ends.
    public void ResetObject()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.position = initialPosition;
        transform.rotation = Quaternion.identity;
    }

    // Full reset and delegates to ResetObject.
    public void ResetInitialState()
    {
        ResetObject();
    }
}
