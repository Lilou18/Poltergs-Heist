using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] SoundDetection objectsound;
    void Start()
    {
        StartCoroutine(test());


        //liststair = npc.NpcMovementController.CanFindPath();
    }
    public IEnumerator test()
    {
        yield return new WaitForSeconds(1);
        BasicNPCBehaviour npc = GetComponent<BasicNPCBehaviour>();
        if (npc != null)
        {
            print("test");
        }
        print("NPC FLOOR LEVEL " + npc.FloorLevel);
        print("OBJECT FLOOR LEVEL " + objectsound.FloorLevel);
        List<StairController> liststair = new List<StairController>();
        FloorNavigationRequest floorRequest = new FloorNavigationRequest(transform.position, npc.FloorLevel, objectsound.FloorLevel, npc.SpriteRenderer);
        List<StairController> path = FloorNavigation.Instance.FindPathToFloor(floorRequest);

        if (path == null)
        {
            print("path null");
        }
        else
        {
            print("path non null");
            foreach (StairController controller in path)
            {
                print("LESCALIER À PRENDRE EST " + controller.gameObject.name);
            }
        }

        //print("DEUXIÈME TEST");
        //yield return npc.NpcMovementController.ReachFloor(npc.FloorLevel, objectsound.FloorLevel);
        //print("FINI DEUXIÈME TEST");
        //print("TROISIÈME TEST");
        //print("POSITION : " + objectsound.transform.position);
        //yield return npc.NpcMovementController.ReachTarget(objectsound.transform.position, npc.FloorLevel, objectsound.FloorLevel);
        //print("FIN TROISIÈME TEST");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
