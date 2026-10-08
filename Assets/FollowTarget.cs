using UnityEngine;

/// Gắn vào Rain: đi theo target (chỉ vị trí, không dính scale/rotation)
public class FollowTarget : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 15, 0);

    void LateUpdate()
    {
        if (target != null) transform.position = target.position + offset;
    }
}
