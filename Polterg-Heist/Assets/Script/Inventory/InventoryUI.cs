using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventoryUI : MonoBehaviour
{
    // Manages the inventory HUD.
    // - Stealable bar: shows a silhouette for each stealable item in the scene,
    //   the sprite is filled when the player picks it up
    // - Collected item bar: shows icons for key items the player is currently carrying,
    //   hidden automatically when empty

    public static InventoryUI Instance {  get; private set; }   // Singleton

    [SerializeField] private GameObject collectedItemBar;       // Parent container for collected key item icons
    [SerializeField] private GameObject keyUIPrefab;            // Prefab used to display a collected key item icon
    [SerializeField] private GameObject stealableBar;           // Parent container for stealable item silhouettes

    // All stealable items in the scene, sorted by name for consistent display order
    List<StealableBehavior> stealableItemList = new List<StealableBehavior>();

    // Maps each stealable sprite to its UI image.
    // Note: if two different stealable objects share the same sprite, only the first
    // one will be represented in the UI. This is intentional for deduplication.
    // There is never two identical stealable in the same level.
    Dictionary<Sprite, GameObject> stealableUI = new Dictionary<Sprite, GameObject>();

    // Maps each key item to its instantiated UI icon
    Dictionary<KeyItemBehavior, GameObject> keyUI = new Dictionary<KeyItemBehavior, GameObject>();

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // List of all stealable item in the level
        // Sort by name for consistent left-to-right display order in the stealable bar
        stealableItemList = FindObjectsByType<StealableBehavior>(FindObjectsSortMode.InstanceID).OrderBy(obj => obj.name).ToList<StealableBehavior>();
    }

    private void Start()
    {
        SetupStealableBarUI();
        // Hide the collected item bar if there are no key items on startup
        collectedItemBar.SetActive(collectedItemBar.transform.childCount > 0);

        // Register to inventory changes event.
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnStealableChanged += UpdateStealableBarUI;
            InventorySystem.Instance.OnKeyItemChanged += UpdateCollectedItemBarUI;
        }
    }

    // Unregister to inventory changes event.
    private void OnDisable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnStealableChanged -= UpdateStealableBarUI;
            InventorySystem.Instance.OnKeyItemChanged -= UpdateCollectedItemBarUI;
        }
    }

    // Creates a black silhouette image in the stealable bar for each stealable item.
    // There is never two identical stealable in the same level.
    private void SetupStealableBarUI()
    {
        if(stealableItemList.Count == 0)
        {
            Debug.LogWarning("Stealable list is empty");
        }

        foreach(StealableBehavior stealableItem in stealableItemList)
        {
            Sprite sprite = stealableItem.ItemSpriteRenderer.sprite;

            if (!stealableUI.ContainsKey(sprite))
            {
                // Create a black silhouette that is filled when the item is picked up
                GameObject obj = new GameObject("StealableObj");
                obj.AddComponent<Image>();
                Image objectImage = obj.GetComponent<Image>();
                objectImage.sprite = stealableItem.ItemSpriteRenderer.sprite;
                objectImage.color = Color.black;
                objectImage.preserveAspect = true;
                obj.transform.SetParent(stealableBar.transform, false);

                stealableUI[sprite] = obj;
            }
        }
    }

    // Updates the stealable bar icon for the given item.
    // Filled when picked up, black when reset.
    private void UpdateStealableBarUI(StealableBehavior itemStolen, bool isPickedUp)
    {
        Sprite sprite = itemStolen.ItemSpriteRenderer.sprite;
        if (stealableUI.ContainsKey(sprite))
        {
            Color colorItem = isPickedUp == true ? Color.white : Color.black;
            stealableUI[sprite].GetComponent<Image>().color = colorItem;
        }
    }

    // Adds or removes a key item icon from the collected item bar.
    // The bar is shown or hidden automatically after the change.
    private void UpdateCollectedItemBarUI(KeyItemBehavior keyitem, bool isPickedUp)
    {
        if (isPickedUp)
        {
            // Instantiate a new icon for the collected key item
            GameObject newKeyImage = Instantiate(keyUIPrefab, collectedItemBar.transform);
            Image collectedItemImage = newKeyImage.GetComponent<Image>();
            collectedItemImage.sprite = keyitem.ItemSpriteRenderer.sprite;
            collectedItemImage.color = keyitem.ItemSpriteRenderer.color;

            keyUI[keyitem] = newKeyImage;
        }
        else
        {
            // Remove key from inventory
            if (keyUI.ContainsKey(keyitem))
            {
                Destroy(keyUI[keyitem]);
                keyUI.Remove(keyitem);
            }           
        }
        // Wait one frame for Destroy to take effect before checking child count
        StartCoroutine(CheckCollectedItemBarEmpty());       
    }

    // Waits one frame then shows or hides the collected item bar based on whether it has children.
    // The one frame delay is necessary because Destroy is deferred until end of frame.
    private IEnumerator CheckCollectedItemBarEmpty()
    {
        yield return null;
        // Verify is the collected item bar still has some image
        collectedItemBar.SetActive(collectedItemBar.transform.childCount > 0);
    }
}
