using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
//using UnityEngine.UIElements;

public class UIPupilFollowBehavior : MonoBehaviour
{
    RectTransform pupilTrans;
    RectTransform eyeTrans;
    Camera cam;
    Scene curScene;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pupilTrans = this.GetComponent<RectTransform>();
        eyeTrans = this.transform.parent.GetComponent<RectTransform>();
        cam = Camera.main;
        curScene = SceneManager.GetActiveScene();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 eyePos = eyeTrans.position;
        Vector3 pupilPos = cam.ScreenToWorldPoint(Input.mousePosition); //= Input.mousePosition;
        //if (curScene.name.Contains("Niveau")) { pupilPos = cam.ScreenToWorldPoint(Input.mousePosition); }
        //else { pupilPos = Input.mousePosition; }

        pupilPos.z = 0;
        Vector3 closestPt = ClosestPoint(pupilPos, eyePos);
        float distanceXFromCenter = eyePos.x - pupilPos.x;
        float distanceYFromCenter = eyePos.y - pupilPos.y;

        if (distanceXFromCenter < 0 && pupilPos.x >= closestPt.x || distanceXFromCenter > 0 && pupilPos.x <= closestPt.x)
        {
            pupilPos.x = closestPt.x;
        }
        if (distanceYFromCenter < 0 && pupilPos.y >= closestPt.y || distanceYFromCenter > 0 && pupilPos.y <= closestPt.y)
        {
            pupilPos.y = closestPt.y;
        }

        pupilTrans.position = pupilPos;
    }

    Vector3 ClosestPoint(Vector3 target, Vector3 center)
    {
        float radius = (Mathf.Min(eyeTrans.lossyScale.x, eyeTrans.lossyScale.y) * Mathf.Min(pupilTrans.localScale.x, pupilTrans.localScale.y));
        Vector3 direction = new Vector3(target.x - center.x, target.y - center.y, 0).normalized;
        Vector3 point = center + (direction * radius);
        point.z = 0;
        return point;
    }
}
