using UnityEngine;

public enum ObjectState
{
    Normal,
    Falling,
    BeingReplaced
}


public class FallingObject : SoundDetection, IPossessable, IResetObject, IResetInitialState
{
    // Behaviour of the falling object when he's possessed

    protected Vector2 initialPosition;    // Initial Position before Falling
    private Rigidbody2D rb;
    private Collider2D objectCollider;

    public ObjectState State { get; private set; }


    
    protected override void Start()
    {
        base.Start();
        initialPosition = transform.position;
        rb= GetComponent<Rigidbody2D>();
        objectCollider= GetComponent<Collider2D>();
        State = ObjectState.Normal;
        objectType = SoundEmittingObject.FallingObject;
    }
    public void OnPossessed()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;  // To make the object fall down
    }
    public void OnDepossessed()
    {
        //throw new System.NotImplementedException();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // When the object collide with the floor it makes a sound that alert nearby enemies.
        if (collision.gameObject.CompareTag("Floor"))
        {
            audioSource.Play();
            NotifyNearbyEnemies(this);
        }
    }

    // Replace the object to it's normal position
    public void ResetObject()
    {
        ReplaceObject();
        // Animation ici
        FinishReplacement();
    }

    // Reset the object state and physics
    private void ReplaceObject()
    {
        State = ObjectState.BeingReplaced;
        rb.bodyType= RigidbodyType2D.Kinematic;
    }
    // Reset the object position and rotation
    private void FinishReplacement()
    {
        State = ObjectState.Normal;
        transform.position = initialPosition;
        transform.rotation = Quaternion.identity;
    }

    public void ResetInitialState()
    {
        ResetObject();
    }
}
