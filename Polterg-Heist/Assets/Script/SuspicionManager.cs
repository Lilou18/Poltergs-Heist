using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SuspicionManager : MonoBehaviour
{
    // This class manage the suspicion of the NPCs

    // Singleton
    public static SuspicionManager Instance { get; private set; }

    private int paranormalObserverCount;    // Number of NPC who sees an object moving
    private float currentSuspicion;
    private float maxSuspicion;
    private float timeSinceSuspicionIncrease;
    [SerializeField] private float timeUntilSuspicionDecrease;

    [SerializeField] protected float suspicionRate; // How much the npc becomes suspicious when the npc sees an object
    [SerializeField] protected float sizeFactor;    // Factor that increases suspicion depending on the size of the object.
    [SerializeField] protected float npcFactor;     // Factor that increases suspicion depending on the amount of NPC watching
    [SerializeField] protected float displacementFactor;    //Factor that increases suspicion depending on how much an objet moved.

    // Position suspicion change variables
    [SerializeField] protected float maxPositionChange; // Maximum distance before npc becomes most suspicious
    [SerializeField] protected float maxPositionFactor; // Factor that increases suspicious when an objet has moved too much
    [SerializeField] protected float minPositionChange; // Minimum distance where the npc notices that the object has moved
    [SerializeField] protected float minPositionFactor; // Factor that increases suspicious when an object has moved

    // Rotation suspicion change variables
    [SerializeField] protected float maxRotationChange; // Maximum rotation an object can have before the NPC get highly suspicious
    [SerializeField] protected float maxRotationFactor; // Factor that increases suspicious when an object has rotated too much
    [SerializeField] protected float minRotationChange; // Minum rotation where the npc that the object has rotated.
    [SerializeField] protected float minRotationFactor; // Factor taht increases suspicious when an object has rotated

    [SerializeField] protected float suspicionDecrease;

    GameObject player;

    public event Action<float> OnSuspicionChanged;  // Event called when the suspicious changed

    bool hasRespawn = false;
    bool hasSuspicionDecrease = false;

    // Getters
    public float CurrentSuspicion => currentSuspicion;
    public bool HasSuspicionDecrease => hasSuspicionDecrease;
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
        paranormalObserverCount = 0;
        currentSuspicion = 0f;
        maxSuspicion = 100f;
        timeSinceSuspicionIncrease = Time.time;
        player = GameObject.FindWithTag("Player");
    }

    private void Update()
    {
        //print(currentSuspicion);
        //UpdateSuspicion();
        if (currentSuspicion >= maxSuspicion && !hasRespawn)
        {
            // We don't want the player to be able to move anymore
            PlayerController playerController = player.GetComponent<PlayerController>();
            playerController.canMove = false;
            // If the player is possessing an object we don't want him to be able to move anymore
            if (playerController.isPossessing)
            {
                if(playerController.lastPossession.GetComponent<PossessionController>() != null)
                {
                    playerController.lastPossession.GetComponent<PossessionController>().canMove = false;
                }                
            }
            hasRespawn = true;
            StartCoroutine(WaitDying());
        }

        if (Time.time - timeSinceSuspicionIncrease >= timeUntilSuspicionDecrease && !hasRespawn)
        {
            currentSuspicion -= suspicionDecrease;

            if (currentSuspicion < 0f)
            {
                currentSuspicion = 0f;
            }
            else
            {
                hasSuspicionDecrease = true;
            }

            OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion);
        }
        else
        {
            hasSuspicionDecrease = false;
        }
    }

    public bool IsPlayerDead()
    {
        return currentSuspicion >= maxSuspicion;
    }

    public void ResetSuspicion()
    {
        print("RESET SUSPICION");
        currentSuspicion = 0f;
        OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion);
        hasRespawn = false;
        hasSuspicionDecrease = false;
    }

    // Record how many NPCs witness a moving object
    public void AddParanormalObserver()
    {
        paranormalObserverCount++;
    }

    // Removes from the registry the NPC who does not look anymore at the moving object
    public void RemoveParanormalObserver()
    {        
        paranormalObserverCount--;        
        paranormalObserverCount = Mathf.Max(0, paranormalObserverCount);
    }

    // When the NPC see Polterg from a mirror or from being a exorcist he dies instantly
    public void UpdateSeeingPoltergSuspicion()
    {
        print("100%");
        currentSuspicion = 100;
        OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion);
    }

    private IEnumerator WaitDying()
    {
        yield return new WaitForSeconds(2f);
        PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        ScoreManager.Instance.AddDeath();
        if(CheckpointManager.Instance.CurrentCheckpoint != null)
        {
            CheckpointManager.Instance.Respawn();
        }
        else
        {
            SceneManager.LoadScene("GameOver");
        }
        
        //
    }

    // Update suspicion when an NPC notices an objet moving in front of them
    public void UpdateMovementSuspicion(float objectSize)
    {
        // Caculate suspicion when an object is moving
        if (paranormalObserverCount > 0)
        {
            // Double the increase of suspicion when multiple NPC see the object moves
            npcFactor = paranormalObserverCount > 1 ? npcFactor : 1f;
            currentSuspicion += suspicionRate * npcFactor * Time.deltaTime + (sizeFactor * objectSize) * Time.deltaTime;
            currentSuspicion = Mathf.Clamp(currentSuspicion, 0f, maxSuspicion);
            OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion); // Change the UI
            timeSinceSuspicionIncrease = Time.time;
        }
    }

    // Update suspicion when an NPC notices an object has moved
    public void UpdateDisplacementSuspicion(float objectSize, float rotationChange, float positionChange)
    {
        print("PositionChange: " + positionChange);
        float positionFactor = 1f;
        // If the object moved too much from it's initial position
        // Becomes highly suspicious
        if(positionChange > maxPositionChange)
        {
            positionFactor = maxPositionFactor;
        }
        // If the object moved fron it's initial position
        // Becomes slightly suspicious
        else if(positionChange > minPositionChange)
        {
            positionFactor = minPositionFactor;
        }

        float rotationFactor = 1f;
        // If the object rotated too much from it's initial rotation
        // Becomes highly suspicious
        if (rotationChange > maxRotationChange)
        {
            rotationFactor = maxRotationFactor;
        }
        // If the object rotated fron it's initial rotation
        // Becomes slightly suspicious
        else if (rotationChange > minRotationChange)
        {
            rotationFactor = minRotationFactor;
        }


        //float positionFactor = Mathf.Clamp(positionChange / maxPositionChange, 1f, 5f);
        //float rotationFactor = Mathf.Clamp(rotationChange / maxRotationChange, 1f, 3f);
        
        float changeFactor = Mathf.Max(positionFactor, rotationFactor);
        print("Change factor : " + changeFactor);
        print("Current suspicion: " + currentSuspicion);
        print("size " + (sizeFactor * objectSize));
        currentSuspicion += displacementFactor * (sizeFactor * objectSize) * changeFactor;
        print(currentSuspicion);
        OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion);    // Change the UI
        timeSinceSuspicionIncrease = Time.time;
    }
}
