using UnityEngine;

public class BallAnimationManager : MonoBehaviour
{
    public float rotationSpeed;
    //ContactPoint2D[] tempContacts;

    //Associated gameObjects
    PossessionController posCon;
    Rigidbody2D rBody;

    private void OnEnable()
    {
        //tempContacts = new ContactPoint2D[10];

        posCon = this.GetComponentInParent<PossessionController>();
        /*if (this.transform.parent.TryGetComponent<PossessionController>(out posCon)) { }
        else if (this.transform.parent.parent.TryGetComponent<PossessionController>(out posCon)) { }
        else { Debug.Log("No PossessionController for the ball"); }*/

        if (posCon.TryGetComponent<Rigidbody2D>(out rBody)) { }
        else { Debug.Log("No Rigidbody2D on the gameObject with the PossessionController for the ball"); }
    }

    private void FixedUpdate()
    {
        if (rBody != null)
        {
            float velocityX = rBody.linearVelocityX;
            bool hasLateralContact = false;
            for (int i = 0; i < posCon.curContact.Count; i++)
            {
                if (posCon.curContact[i].normal.x > .9f || posCon.curContact[i].normal.x < -.9f)
                {
                    hasLateralContact = true;
                    break;
                }

                /*for (int ii = 0; ii < posCon.curObject[i].GetComponent<Collider2D>().GetContacts(tempContacts); ii++)
                {
                    if (tempContacts[ii].normal.x > .9f || tempContacts[ii].normal.x < -.9f)
                    {
                        hasLateralContact = true;
                        break;
                    }
                }
                if (hasLateralContact)
                    break;*/
            }
            
            if (!hasLateralContact)
            {
                if (velocityX > 0)
                    transform.Rotate(new Vector3(0, 0, rotationSpeed * -velocityX), Space.Self);
                else if (velocityX < 0)
                    transform.Rotate(new Vector3(0, 0, rotationSpeed * -velocityX), Space.Self);
            }
        }
    }
}
