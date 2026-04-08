using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SuspicionManager : MonoBehaviour
{
    // Tracks the global suspicion level raised by NPCs witnessing paranormal activity.
    // Suspicion increases when NPCs see objects moving or displaced, and decreases
    // over time when no activity is observed. When suspicion reaches 100, the player
    // dies.

    // Singleton
    public static SuspicionManager Instance { get; private set; }

    private int paranormalObserverCount;                        // Number of NPC who sees an object moving

    private float currentSuspicion;                             // The amount of suspicion shared by all NPCs.
    private float maxSuspicion;                                 // Maximum amount of suspicion
    private float timeSinceSuspicionIncrease;                   // Time since the last suspicion increase
    [SerializeField] private float timeUntilSuspicionDecrease;  // How many seconds must pass without suspicion increasing before it starts decreasing

    [SerializeField] protected float suspicionRate;             // How much the npc becomes suspicious when the npc sees an object
    [SerializeField] protected float sizeFactor;                // Factor that increases suspicion depending on the size of the object.
    [SerializeField] protected float npcFactor;                 // Factor that increases suspicion depending on the amount of NPC watching
    [SerializeField] protected float displacementFactor;        //Factor that increases suspicion depending on how much an objet moved.

    // Position suspicion change variables
    [SerializeField] protected float maxPositionChange;         // Maximum distance before npc becomes most suspicious
    [SerializeField] protected float maxPositionFactor;         // Factor that increases suspicious when an objet has moved too much
    [SerializeField] protected float minPositionChange;         // Minimum distance where the npc notices that the object has moved
    [SerializeField] protected float minPositionFactor;         // Factor that increases suspicious when an object has moved

    // Rotation suspicion change variables
    [SerializeField] protected float maxRotationChange;         // Maximum rotation an object can have before the NPC get highly suspicious
    [SerializeField] protected float maxRotationFactor;         // Factor that increases suspicious when an object has rotated too much
    [SerializeField] protected float minRotationChange;         // Minimum rotation where the npc that the object has rotated.
    [SerializeField] protected float minRotationFactor;         // Factor that increases suspicious when an object has rotated

    [SerializeField] protected float suspicionDecrease;         // Amount of suspicion removed per second

    GameObject player;

    public event Action<float> OnSuspicionChanged;              // Event called when the suspicious changed

    bool hasRespawn = false;                                    // True once the death sequence has started, prevents triggering it multiple times
    bool hasSuspicionDecrease = false;                          // True when suspicion is actively decreasing

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
        // Trigger the death sequence once when suspicion hits max
        if (currentSuspicion >= maxSuspicion && !hasRespawn)
        {
            // Freeze the player's movement
            PlayerController playerController = player.GetComponent<PlayerController>();
            playerController.canMove = false;
            // Also freeze the possessed object if the player is currently possessing one
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

        // Decrease suspicion if enough time has passed since the last increase
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

    // Returns true if suspicion has reached its maximum value.
    public bool IsPlayerDead()
    {
        return currentSuspicion >= maxSuspicion;
    }

    // Resets suspicion to zero and clears all death/decrease flags.
    public void ResetSuspicion()
    {
        currentSuspicion = 0f;
        OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion);
        hasRespawn = false;
        hasSuspicionDecrease = false;
    }

    // Registers an NPC as currently witnessing a moving object.
    public void AddParanormalObserver()
    {
        paranormalObserverCount++;
    }

    // Unregisters an NPC that stopped witnessing a moving object.
    public void RemoveParanormalObserver()
    {        
        paranormalObserverCount--;        
        paranormalObserverCount = Mathf.Max(0, paranormalObserverCount);
    }

    // Reach max suspicion instantly when NPCs sees Polterg (trough a mirror
    // or when the NPC is an exorcist), causing immediate death.
    public void UpdateSeeingPoltergSuspicion()
    {
        currentSuspicion = 100;
        OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion);
    }

    // Waits for the death animation to finish, then records the death and either
    // respawns the player at the last checkpoint or loads the Game Over screen.
    private IEnumerator WaitDying()
    {
        yield return new WaitForSeconds(2f);

        PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        // Record the number of deaths for the score
        ScoreManager.Instance.AddDeath();

        if(CheckpointManager.Instance.CurrentCheckpoint != null)
        {
            CheckpointManager.Instance.Respawn();
        }
        else
        {
            SceneManager.LoadScene("GameOver");
        }
    }

    // Increases suspicion each frame while at least one NPC sees an object moving.
    // The increase scales with object size and the number of watching NPCs.
    public void UpdateMovementSuspicion(float objectSize)
    {
        // Caculate suspicion when an object is moving
        if (paranormalObserverCount > 0)
        {
            // Apply the npcFactor multiplier only when more than one NPC is watching
            npcFactor = paranormalObserverCount > 1 ? npcFactor : 1f;
            currentSuspicion += suspicionRate * npcFactor * Time.deltaTime + (sizeFactor * objectSize) * Time.deltaTime;
            currentSuspicion = Mathf.Clamp(currentSuspicion, 0f, maxSuspicion);

            // Update the Suspicion bar UI value
            OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion); 
            timeSinceSuspicionIncrease = Time.time;
        }
    }

    // Increases suspicion when an NPC notices that an object has been displaced from its original position or rotation.
    // The increase scales with object size and how far the object has moved or rotated,
    // using the higher of the two displacement factors.
    public void UpdateDisplacementSuspicion(float objectSize, float rotationChange, float positionChange)
    {
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
        
        float changeFactor = Mathf.Max(positionFactor, rotationFactor);
        currentSuspicion += displacementFactor * (sizeFactor * objectSize) * changeFactor;
        OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion);    // Update the Suspicion bar UI value
        timeSinceSuspicionIncrease = Time.time;
    }
}
