using UnityEngine;

public class RaycastForColliders : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
            foreach (RaycastHit2D hit in hits)
            {
                print(hit.collider.transform.name);
            }
        }
    }
}
