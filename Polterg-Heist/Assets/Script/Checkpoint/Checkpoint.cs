using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public interface IResetInitialState
{
    void ResetInitialState();
}

public class Checkpoint : MonoBehaviour
{
    // This class manage the reset of objets and enemies when the player dies
    // for some specefic part of the game
    [SerializeField] public AK.Wwise.Event checkPointSound;
    [SerializeField] private Sprite unlitLanternSprite;
    [SerializeField] private Sprite litLanternSprite;
    List<IResetInitialState> resetGameObject;   // All objects and enemies that must be reset when dying at this checkpoint
    private Light2D checkpointLight;
    private Collider2D checkpointCollider;
    private SpriteRenderer sr;
    // Getters
    public List<IResetInitialState> ResetGameObjects => resetGameObject;

    private void Start()
    {
        checkpointCollider = GetComponent<Collider2D>();
        checkpointLight = GetComponentInChildren<Light2D>();//transform.GetChild(0).gameObject;
        sr = GetComponentInChildren<SpriteRenderer>();
        Vector3 initialPosition = new Vector3(transform.position.x, transform.position.y, 0);
        transform.position = initialPosition;
        checkpointLight.enabled = false;
        FindAllResetObjects();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PossessionManager possessionManager = collision.GetComponent<PossessionManager>();
        if (collision.gameObject.GetComponent<PlayerController>() || (possessionManager != null && possessionManager.IsPossessing))
        {
            checkpointCollider.enabled = false; // The player can't reactivate the same checkpoint
            CheckpointManager.Instance.SetCheckPoint(this);
            checkpointLight.enabled = true;//SetActive(true);
            checkPointSound.Post(gameObject);
            sr.sprite = litLanternSprite;
        }
    }

    // Find all the objects that must be reset when failling a challenge
    private void FindAllResetObjects()
    {
        resetGameObject = transform.parent.GetComponentsInChildren<IResetInitialState>(true).ToList();
        //foreach (var obj in resetGameObject)
        //{
        //    MonoBehaviour component = obj as MonoBehaviour; // Cast en MonoBehaviour
        //    if (component != null)
        //    {
        //        Debug.Log(component.gameObject.name);
        //    }
        //}
        //print(resetGameObject.Count);
    }
}
