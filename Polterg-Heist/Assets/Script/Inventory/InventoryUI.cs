using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance {  get; private set; }

    [SerializeField] private GameObject collectedItemBar;
    [SerializeField] private GameObject keyUIPrefab;
    [SerializeField] private GameObject stealableBar;

    List<StealableBehavior> stealableItemList = new List<StealableBehavior>();  // List of every stealable items in the scene
    Dictionary<Sprite, GameObject> stealableUI = new Dictionary<Sprite, GameObject>();  // Each treasure has it's own sprite
    Dictionary<KeyItemBehavior, GameObject> keyUI = new Dictionary<KeyItemBehavior, GameObject>();

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance);
        }

        stealableItemList = FindObjectsByType<StealableBehavior>(FindObjectsSortMode.InstanceID).OrderBy(obj => obj.name).ToList<StealableBehavior>();
    }

    private void Start()
    {
        SetupStealableBarUI();
        collectedItemBar.SetActive(collectedItemBar.transform.childCount > 0);
    }

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
                // Add to the inventory the shadow of the picture to find
                GameObject obj = new GameObject("StealableObj");
                obj.AddComponent<Image>();
                Image objectImage = obj.GetComponent<Image>();
                objectImage.sprite = stealableItem.ItemSpriteRenderer.sprite;
                objectImage.color = Color.black;
                objectImage.preserveAspect = true;
                obj.transform.SetParent(stealableBar.transform, false);

                stealableUI[sprite] = obj;
            }
            

            //stealableUI[stealableItem] = obj;
        }
    }

    // Update the stealable bar UI when an item is stolen or when we reset
    public void UpdateStealableBarUI(StealableBehavior itemStolen, bool isPickedUp)
    {
        Sprite sprite = itemStolen.ItemSpriteRenderer.sprite;
        if (stealableUI.ContainsKey(sprite))
        {
            Color colorItem = isPickedUp == true ? Color.white : Color.black;
            stealableUI[sprite].GetComponent<Image>().color = colorItem;
        }
    }

    public void UpdateCollectedItemBarUI(KeyItemBehavior keyitem, bool isPickedUp)
    {
        // Add key to inventory
        if (isPickedUp)
        {
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
        StartCoroutine(CheckCollectedItemBarEmpty());
        
       
    }

    // Check if the collected bar tiem is empty, if yes we desactivate it
    private IEnumerator CheckCollectedItemBarEmpty()
    {
        yield return null;  // Wait for one frame
        // Verify is the collected item bar still has some image
        collectedItemBar.SetActive(collectedItemBar.transform.childCount > 0);
    }
}
