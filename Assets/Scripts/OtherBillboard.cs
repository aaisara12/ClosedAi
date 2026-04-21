
using UnityEngine;

public class OtherBillboard : MonoBehaviour
{
    public void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform.position, Vector3.up);
        }
    }
}
