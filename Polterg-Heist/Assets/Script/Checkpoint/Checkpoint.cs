using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Implement this interface on any object that can be restored to its original state after the player dies.
public interface IResetInitialState
{
    void ResetInitialState();
}

public class Checkpoint : MonoBehaviour
{
    // Represents a single checkpoint in the level.
    // Once activated, it defines the zone the player will respawn in by holding
    // the list of all objects in that zone that must be restored to their initial
    // state upon death. This list is automatically built from every
    // IResetInitialState component found under the parent transform.

    [SerializeField] public AK.Wwise.Event checkPointSound; // Wwise event played when this checkpoint is activated

    [SerializeField] private Sprite unlitLanternSprite;     // Lantern sprite shown before the checkpoint is activated

    [SerializeField] private Sprite litLanternSprite;       // Lantern sprite shown after the checkpoint is activated

    List<IResetInitialState> resetGameObject;               // All objects and enemies that must be reset when dying at
                                                            // this checkpoint

    private Light2D checkpointLight;                        // 2D light representing the lantern flame

    private Collider2D checkpointCollider;                  // Trigger collider
    private SpriteRenderer sr;

    // Getter
    public List<IResetInitialState> ResetGameObjects => resetGameObject;

    private void Start()
    {
        checkpointCollider = GetComponent<Collider2D>();
        checkpointLight = GetComponentInChildren<Light2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        // Force Z position to 0 to avoid 2D rendering/sorting issues
        Vector3 initialPosition = new Vector3(transform.position.x, transform.position.y, 0);
        transform.position = initialPosition;

        // Lantern starts unlit
        checkpointLight.enabled = false;

        // Build the reset list for this checkpoint zone
        FindAllResetObjects();
    }

    // Activates the checkpoint when the player or a possessed object enters the trigger zone.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PossessionManager possessionManager = collision.GetComponent<PossessionManager>();
        // We check if possessed object are really possessed so they do not accidentally trigger the checkpoint
        if (collision.gameObject.GetComponent<PlayerController>() || (possessionManager != null && possessionManager.IsPossessing))
        {
            // The player can't reactivate the same checkpoint
            checkpointCollider.enabled = false;
            // Register this checkpoint as the active respawn point
            CheckpointManager.Instance.SetCheckPoint(this);

            // Visual and audio feedback
            checkpointLight.enabled = true;
            checkPointSound.Post(gameObject);
            sr.sprite = litLanternSprite;
        }
    }

    // Scans the entire parent transform (including inactive children) for all GameObjects
    // that must be reset when failling a challenge. Using the parent means all objects
    // belonging to the same level section are captured, regardless of which child transform
    // they sit under.
    // Disabled objects must also reset because of case like breakable objets.

    private void FindAllResetObjects()
    {
        resetGameObject = transform.parent.GetComponentsInChildren<IResetInitialState>(true).ToList();
    }
}
