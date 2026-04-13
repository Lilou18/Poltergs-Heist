using UnityEngine;
using System.Collections;

public class NPCSpriteManager : MonoBehaviour
{
    // Smoothly rotates the NPC sprite and FOV cone to face the correct direction
    // based on the FacingRight property of the parent BasicNPCBehaviour.

    BasicNPCBehaviour npcBehav;         // Contains important NPC variables such has FacingRight
    Transform fieldOfView;              // FOV cone transform to rotate
    Transform pivot;                    // Sprite pivot transform to rotate
    Animator npcAnim;                   // Parent animator, used to find the pivot transform

    float rotationSpeed = 1000f;        // Degrees per second for sprite and FOV rotation
    float rotationYIni;                 // Initial Y rotation of the pivot, used to determine FOV target angle

    Vector3 directionSprite;            // Target euler angles for the sprite pivot
    Vector3 directionView;              // Target euler angles for the FOV cone


    void Start()
    {        
        npcBehav = this.GetComponentInParent<BasicNPCBehaviour>();
        fieldOfView = npcBehav.transform.Find("NPCLight").transform;
        npcAnim = this.GetComponentInParent<Animator>();
        pivot = npcAnim.transform;
        rotationYIni = pivot.eulerAngles.y;       
    }

    // Reads FacingRight each frame and smoothly rotates the sprite and FOV to match.
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

    // Smoothly rotates both the sprite pivot and FOV cone toward their target angles.
    void RotateSprite(Vector3 sprite, Vector3 view)
    {
        float step = rotationSpeed * Time.deltaTime;
        Vector3 spriteAngle = Vector3.MoveTowards(pivot.eulerAngles, sprite, step);
        pivot.eulerAngles = spriteAngle;

        Vector3 viewAngle = Vector3.MoveTowards(fieldOfView.eulerAngles, view, step);
        fieldOfView.eulerAngles = viewAngle;
    }
}
