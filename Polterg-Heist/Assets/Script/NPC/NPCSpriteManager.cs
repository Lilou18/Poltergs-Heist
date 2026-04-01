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

    void Start()
    {
        
        npcBehav = this.GetComponentInParent<BasicNPCBehaviour>();
        npcTrans = npcBehav.transform;
        fieldOfView = npcTrans.Find("NPCLight").transform;
        npcAnim = this.GetComponentInParent<Animator>();
        pivot = npcAnim.transform;
        rotationYIni = pivot.eulerAngles.y;
       
    }

    // Update is called once per frame
    void Update()
    {        
        bool isFacingRight = npcBehav.FacingRight;
        directionSprite = pivot.eulerAngles;
        directionView = fieldOfView.eulerAngles;

        if (isFacingRight)
        {
            // Look right
            directionSprite.y = 0;
            if (rotationYIni == 180) { directionView.y = 0; }
            else { directionView.y = 180; }    
        }
        else
        {
            // Look left
            directionSprite.y = 180;
            if (rotationYIni == 180) { directionView.y = 180; }
            else { directionView.y = 0; }
        }
        RotateSprite(directionSprite, directionView);
    }

    void RotateSprite(Vector3 sprite, Vector3 view)
    {
        float step = rotationSpeed * Time.deltaTime;
        Vector3 spriteAngle = Vector3.MoveTowards(pivot.eulerAngles, sprite, step);
        pivot.eulerAngles = spriteAngle;

        Vector3 viewAngle = Vector3.MoveTowards(fieldOfView.eulerAngles, view, step);
        fieldOfView.eulerAngles = viewAngle;
    }
}
