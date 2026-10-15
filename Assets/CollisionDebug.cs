using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gắn TẠM vào Player để tìm thứ đang chặn đường: in ra Console tên object mà nhân vật đụng vào
/// (mỗi object chỉ in 1 lần). Bấm vào dòng log → Unity chỉ ra object đó trong Hierarchy.
/// Tìm xong thì gỡ script này ra.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CollisionDebug : MonoBehaviour
{
    [Tooltip("Bỏ qua va chạm với mặt sàn (chỉ log thứ chắn ngang)")]
    [SerializeField] private bool ignoreFloor = true;

    readonly HashSet<Collider> logged = new HashSet<Collider>();

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (ignoreFloor && hit.normal.y > 0.7f) return; // mặt phẳng nằm ngang = sàn
        if (!logged.Add(hit.collider)) return;

        Transform t = hit.collider.transform;
        string path = t.name;
        for (Transform p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
        Debug.Log($"[Va chạm] Đụng vào: {path}  ({hit.collider.GetType().Name})", hit.collider);
    }
}
