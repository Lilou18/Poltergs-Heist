using UnityEngine;
using System.Collections;

public class NPCSpriteManager : MonoBehaviour
{
    BasicNPCBehaviour npcBehav;
    Transform fieldOfView;
    Transform pivot;
    Transform npcTrans;
    Animator npcAnim;

    float rotationSpeed = 1000f;        //Multiplier for the number of degrees to turn each frame
    Vector3 directionSprite;
    Vector3 directionView;
    float rotationYIni;
    //Quaternion directionSprite;
    //Quaternion directionView;
    //Vector3 lastNPCPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //if (this.transform.parent.TryGetComponent<BasicNPCBehaviour>(out npcBehav)) { }
        //else if (this.transform.parent.parent.TryGetComponent<BasicNPCBehaviour>(out npcBehav)) { }
        //else { Debug.Log("No BasicNPCBehaviour in parent or grandparent for a NPC sprite"); }
        npcBehav = this.GetComponentInParent<BasicNPCBehaviour>();
        npcTrans = npcBehav.transform;
        fieldOfView = npcTrans.Find("NPCLight").transform;
        npcAnim = this.GetComponentInParent<Animator>();
        pivot = npcAnim.transform;
        rotationYIni = pivot.eulerAngles.y;
        //lastNPCPos = npcTrans.position;
    }

    // Update is called once per frame
    void Update()
    {
        /*if (npcBehav.transform.name != "Cat")
        {
            Vector3 currentPos = npcTrans.position;
            if (lastNPCPos.x != currentPos.x)
            {
                npcAnim.SetBool("InMovement", true);
                if (currentPos.x - lastNPCPos.x > 0)
                {
                    npcAnim.SetFloat("Direction", 1);
                }
                else
                {
                    npcAnim.SetFloat("Direction", -1);
                }
            }
            else
            {
                npcAnim.SetBool("InMovement", false);
            }
        }*/
        
        bool isFacingRight = npcBehav.FacingRight;
        directionSprite = pivot.eulerAngles;
        directionView = fieldOfView.eulerAngles;

        if (isFacingRight)
        {
            directionSprite.y = 0;
            if (rotationYIni == 180) { directionView.y = 0; }
            else { directionView.y = 180; }
            //directionSprite = new Quaternion(0, 0, 0, 1);     //Look right
            //directionView = new Quaternion(.70711f, .70711f, 0, 0);
        }
        else
        {
            directionSprite.y = 180;
            if (rotationYIni == 180) { directionView.y = 180; }
            else { directionView.y = 0; }
            //directionSprite = new Quaternion(0, 1, 0, 0);     //Look left
            //directionView = new Quaternion(0, 0, .70711f, .70711f);
        }

        RotateSprite(directionSprite, directionView);
        //lastNPCPos = npcTrans.position;
    }

    void RotateSprite(Vector3 sprite, Vector3 view)
    {
        float step = rotationSpeed * Time.deltaTime;
        Vector3 spriteAngle = Vector3.MoveTowards(pivot.eulerAngles, sprite, step);
        pivot.eulerAngles = spriteAngle;

        Vector3 viewAngle = Vector3.MoveTowards(fieldOfView.eulerAngles, view, step);
        fieldOfView.eulerAngles = viewAngle;
    }

    /*void RotateSprite(Quaternion sprite, Quaternion view)
    {
        float step = rotationSpeed * Time.deltaTime;
        Quaternion spriteAngle = Quaternion.RotateTowards(pivot.localRotation, sprite, step);
        pivot.rotation = spriteAngle;

        Quaternion viewAngle = Quaternion.RotateTowards(fieldOfView.localRotation, view, step);
        fieldOfView.rotation = viewAngle;
    }*/
}
