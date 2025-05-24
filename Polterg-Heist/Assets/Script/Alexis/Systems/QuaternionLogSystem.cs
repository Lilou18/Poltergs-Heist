using UnityEngine;

[ExecuteInEditMode]
public class QuaternionLogSystem : MonoBehaviour
{
    private void Update()
    {
        Quaternion euleurToQuat = this.transform.rotation;
        Debug.Log(this.transform.name + ": " + euleurToQuat);
    }
}
