using System.Collections;
using System.Linq;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    // Handles respawning of player and other objects

    public static CheckpointManager Instance { get; private set; }  // Singleton
    private Checkpoint currentCheckpoint;    // Current checkpoint when player dies
    private GameObject player;
    [SerializeField] private bool resetAll = false;
    [SerializeField] private GameObject[] resetGameObject;
    BasicNPCBehaviour[] allNPCs;
    JukeBox[] allJukeBox;
    

    // Tempory getters
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
        if (resetAll)
        {
            StartCoroutine(FindAllNPCs());
            StartCoroutine(FindAllJukeBox());
        }

    }

    private IEnumerator FindAllNPCs()
    {
        yield return new WaitForSeconds(0.2f);
        allNPCs = GameObject.FindObjectsByType<BasicNPCBehaviour>(FindObjectsSortMode.None);
        print(allNPCs.Length);
    }

    private IEnumerator FindAllJukeBox()
    {
        yield return new WaitForSeconds(0.2f);
        allJukeBox = GameObject.FindObjectsByType<JukeBox>(FindObjectsSortMode.None);
        print(allJukeBox.Length);
    }

    private void ResetAll()
    {
        if (resetAll)
        {
            foreach (BasicNPCBehaviour npc in allNPCs)
            {
                npc.ResetInitialState();
            }

            foreach (JukeBox box in allJukeBox)
            {
                box.ResetInitialState();
            }
        }
    }

    private void ResetObjects()
    {
        print("RESETOBJECTS");
        if (resetGameObject != null)
        {
            foreach (GameObject go in resetGameObject)
            {
                PossessionManager posssessManager = go.GetComponentInChildren<PossessionManager>();
                if (posssessManager != null)
                {
                    print("TESTING RESET" +  go.name);
                    //posssessManager.StopPossession();
                    posssessManager.ResetInitialState();
                }
            }
        }
    }

    public void SetCheckPoint(Checkpoint newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
    }

    public void Respawn()
    {
        if(currentCheckpoint != null)
        {
            print("I HAVE A CHECKPOINT");
            StartCoroutine(WaitBeforeReset());
        }
    }

    private void ResetEnemies()
    {
        HumanNPCBehaviour[] listNPC = GameObject.FindObjectsByType<HumanNPCBehaviour>(FindObjectsSortMode.None);
        foreach (HumanNPCBehaviour npc in listNPC){
            npc.ResetSeePolterg();
        }
    }

    // Reset every game object associated with the checkpoint
    // Reset the player and the enemies suspicion
    private IEnumerator WaitBeforeReset()
    {
        //print("RESET");
        yield return new WaitForSeconds(0.1f);
        //yield return StartCoroutine(ResetInitialStateGameObjects());

        foreach (IResetInitialState resetGameObject in currentCheckpoint.ResetGameObjects)
        {

            MonoBehaviour component = resetGameObject as MonoBehaviour; // Cast en MonoBehaviour
            if (component != null && component.gameObject.activeInHierarchy)
            {
                print(component.gameObject.name);
                resetGameObject.ResetInitialState();    // Reset the game object to it's initial state

            }

            //else
            //{
            //    print(component.gameObject.name);
            //}

        }
        ResetObjects();
        //yield return null;
        if (player != null)
        {
            player.transform.position = currentCheckpoint.transform.position;
            player.GetComponent<PlayerController>().canMove = true;
        }


        ResetEnemies();
        ResetAll();
        SuspicionManager.Instance.ResetSuspicion();
    }

    // Reset every game object associated with the checkpoint
    private IEnumerator ResetInitialStateGameObjects()
    {
        foreach (IResetInitialState resetGameObject in currentCheckpoint.ResetGameObjects)
        {

            MonoBehaviour component = resetGameObject as MonoBehaviour; // Cast en MonoBehaviour
            if (component != null && component.gameObject.activeInHierarchy)
            {
                print(component.gameObject.name);
                resetGameObject.ResetInitialState();    // Reset the game object to it's initial state

            }

            //else
            //{
            //    print(component.gameObject.name);
            //}

        }
        yield return null;
    }


}
