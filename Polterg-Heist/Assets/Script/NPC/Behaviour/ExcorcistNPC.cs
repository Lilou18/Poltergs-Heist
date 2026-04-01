using UnityEngine;

public class ExcorcistNPC : PatrollingNPCBehaviour
{
    protected override void DetectMovingObjects()
    {
        isObjectMoving = false;
        float objectSize = 0f;

        // Find all the possible possessed object in the room
        Collider2D[] objects = Physics2D.OverlapCircleAll(transform.position, detectionRadius, detectObjectLayer);
        foreach (Collider2D obj in objects)
        {

            if (this.IsObjectInFieldOfView(obj))
            {
                // Check if there is no object blocking the sight of the NPC
                RaycastHit2D hit = Physics2D.Raycast(transform.position, (obj.transform.position - transform.position).normalized, detectionRadius, ~ignoreLayerSightBlocked);

                // Is the path from the npc to the object clear?
                if (hit.collider != null && hit.collider == obj)
                {
                    PlayerController player = obj.GetComponent<PlayerController>();
                    if(player != null)
                    {
                        if (!seePolterg)
                        {
                            seePolterg = true;
                            NPCSeePolterg();
                        }
                        //audioSource.PlayOneShot(audioSource.clip);
                        //print("audio source play");
                        
                        //SuspicionManager.Instance.UpdateSeeingPoltergSuspicion();
                        return;
                        
                    }
                    else
                    {
                        // Get object size
                        Renderer objRenderer = obj.GetComponent<Renderer>();
                        objectSize = Mathf.Max(objRenderer.bounds.size.x, objRenderer.bounds.size.y);

                        // Check if the object is moving
                        PossessionController possessedObject = obj.GetComponent<PossessionController>();

                        if (possessedObject != null)
                        {
                            // Check if the object is moving in front of him
                            if (possessedObject.IsMoving)
                            {
                                isObjectMoving = true;
                            }

                            HandleChangedPositionSuspicion(possessedObject, objectSize);
                        }
                    }
                }
            }
        }
        HandleMovementSuspicion(objectSize);
    }



}
