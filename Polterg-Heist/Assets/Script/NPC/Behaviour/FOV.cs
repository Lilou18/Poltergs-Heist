using System.Collections.Generic;
using UnityEngine;

public class FOV : MonoBehaviour
{
    public float viewRadius = 5f;
    public float viewAngle = 90f;
    public int rayCount = 50;
    public LayerMask obstacleMask;

    private Mesh mesh;
    private Vector3 origin;

    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        origin = Vector3.zero;
    }

    private void LateUpdate()
    {
        origin = transform.position;
        float angle = -viewAngle / 2f;
        float angleIncrease = viewAngle / rayCount;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        vertices.Add(Vector3.zero); // Centre du FOV

        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 dir = DirFromAngle(angle, true);
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, viewRadius, obstacleMask);

            if (hit)
            {
                vertices.Add(transform.InverseTransformPoint(hit.point));
            }
            else
            {
                vertices.Add(transform.InverseTransformPoint(origin + dir * viewRadius));
            }

            angle += angleIncrease;
        }

        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool global)
    {
        if (!global)
            angleInDegrees += transform.eulerAngles.z;
        return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad));
    }
}
