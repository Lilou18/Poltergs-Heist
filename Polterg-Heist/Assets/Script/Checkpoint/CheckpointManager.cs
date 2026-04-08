using System.Collections;
using System.Linq;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    // Manages checkpoint saving and player/object respawning logic.
    // Works in tandem with Checkpoint which registers itself here when activated.

    [SerializeField] private bool resetAll = false;                 // If true, all BasicNPCBehaviours and Gramophones in
                                                                    // the scene are reset on respawn, regardless of which
                                                                    // checkpoint is active

    [SerializeField] private GameObject[] resetPossessedGameObject; // Specific possessed GameObjects to reset on respawn,
                                                                    // regardless of which checkpoint is active
    public static CheckpointManager Instance { get; private set; }  // Singleton

    private Checkpoint currentCheckpoint;                           // The last checkpoint activated by the player

    private GameObject player;                                      // Reference to the player GameObject

    BasicNPCBehaviour[] allNPCs;                                    // All BasicNPCBehaviour instances in the scene

    Gramophone[] allGramophone;                                     // All Gramophone instances in the scene


    // Getter
    public Checkpoint CurrentCheckpoint { get { return currentCheckpoint; } }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        // Only scan the scene for NPCs/Gramophones if a full reset is required
        if (resetAll)
        {
            StartCoroutine(FindAllNPCs());
            StartCoroutine(FindAllGramophone());
        }

    }

    // Collects every BasicNPCBehaviour in the scene.
    private IEnumerator FindAllNPCs()
    {
        yield return new WaitForSeconds(0.2f);
        allNPCs = GameObject.FindObjectsByType<BasicNPCBehaviour>(FindObjectsSortMode.None);
    }

    // Collects every Gramophone in the scene.
    private IEnumerator FindAllGramophone()
    {
        yield return new WaitForSeconds(0.2f);
        allGramophone = GameObject.FindObjectsByType<Gramophone>(FindObjectsSortMode.None);
    }

    // Resets all NPCs and Gramophone in the scene to their initial state.
    // Only runs if resetAll is enabled in the inspector.
    // Complements the targeted reset done on the current checkpoint's object list.
    private void ResetAll()
    {
        if (resetAll)
        {
            foreach (BasicNPCBehaviour npc in allNPCs)
            {
                npc.ResetInitialState();
            }

            foreach (Gramophone gramophone in allGramophone)
            {
                gramophone.ResetInitialState();
            }
        }
    }

    // Resets every GameObject listed in resetPossessedGameObject.
    // This targets objects that are not children of any checkpoint's parent transform
    // and therefore not picked up by Checkpoint.FindAllResetObjects.
    private void ResetPossessedObjects()
    {
        if (resetPossessedGameObject != null)
        {
            foreach (GameObject go in resetPossessedGameObject)
            {
                PossessionManager posssessManager = go.GetComponentInChildren<PossessionManager>();
                if (posssessManager != null)
                {
                    posssessManager.ResetInitialState();
                }
            }
        }
    }

    // Registers a checkpoint as the current active one.
    public void SetCheckPoint(Checkpoint newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
    }

    // Triggers the full respawn sequence if a checkpoint has been registered.
    // Called externally when the player dies by SuspicionManager.
    public void Respawn()
    {
        if(currentCheckpoint != null)
        {
            StartCoroutine(WaitBeforeReset());
        }
    }


    // Resets the suspicion state of every HumanNPCBehaviour in the scene.
    // Called after respawn so enemies no longer remember spotting the player.
    private void ResetEnemies()
    {
        HumanNPCBehaviour[] listNPC = GameObject.FindObjectsByType<HumanNPCBehaviour>(FindObjectsSortMode.None);
        foreach (HumanNPCBehaviour npc in listNPC){
            npc.ResetSeePolterg();
        }
    }

    // Reset every GameObject associated with the checkpoint
    // Reset possessed objects from resetPossessedGameObject
    // Resets enemy suspicion and, optionally, all scene NPCs/Gramophones.
    // Teleports the player to the checkpoint position and re-enables movement.
    private IEnumerator WaitBeforeReset()
    {
        // Short delay to let any death animation / effect finish.
        yield return new WaitForSeconds(0.1f);

        // Reset every IResetInitialState object linked to the active checkpoint
        foreach (IResetInitialState resetGameObject in currentCheckpoint.ResetGameObjects)
        {

            MonoBehaviour component = resetGameObject as MonoBehaviour;
            if (component != null && component.gameObject.activeInHierarchy)
            {
                resetGameObject.ResetInitialState();    // Reset the game object to it's initial state

            }
        }
        ResetPossessedObjects();

        // Teleport the player to the checkpoint and restore movement
        if (player != null)
        {
            player.transform.position = currentCheckpoint.transform.position;
            player.GetComponent<PlayerController>().canMove = true;
        }


        ResetEnemies();
        ResetAll();     // Only if resetAll is enable
        SuspicionManager.Instance.ResetSuspicion();
    }
}
