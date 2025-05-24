using UnityEngine;


public class StealableBehavior : PickupItemBehavior
{
    EnergySystem energy;
    [SerializeField] public float energyGain;
    public AK.Wwise.Event objectPickUpSoundEvent;
    public AK.Wwise.Event playerYaySoundEvent;


    // Start is called once before the first execution of Update after the MonoBehaviour is created

    protected override void Awake()
    {
        base.Awake();
    }
    protected void Start()
    {
        energy = FindFirstObjectByType<EnergySystem>();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        PossessionManager possessionManager = collision.GetComponent<PossessionManager>();
        if (collision.GetComponent<PlayerController>() != null || (possessionManager != null && possessionManager.IsPossessing))
        {
            InventorySystem.Instance.AddStolenItemToInventory(this);
            //Debug.Log("Collision détectée avec : " + other.gameObject.name);
            playerYaySoundEvent.Post(gameObject);
            objectPickUpSoundEvent.Post(gameObject); // Joue le son
        }
            //energy.ModifyEnergy(energyGain);


  
    }
}
