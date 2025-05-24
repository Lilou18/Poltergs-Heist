using UnityEngine;

public class TriggerZoneCamera : MonoBehaviour
{
    [SerializeField] private Transform limitTopLeft;
    [SerializeField] private Transform limitBottomRight;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PossessionManager possessionManager = collision.GetComponent<PossessionManager>();
        // If Polterg enter the triggers or if Polterg when he possessed an object enter the trigger
        // We update the camera bounds
        if(collision.CompareTag("Player") || (possessionManager != null && possessionManager.IsPossessing))
        {
            CameraBehavior.Instance.UpdateCameraLimits(limitTopLeft, limitBottomRight);
        }
    }
}
