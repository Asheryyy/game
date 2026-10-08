using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu Tools → Add Mesh Colliders: thêm Mesh Collider cho object đang chọn
/// và TẤT CẢ object con có hình (MeshFilter) mà chưa có collider.
/// Bấm Ctrl+Z để hoàn tác.
/// </summary>
public static class AddMeshColliders
{
    [MenuItem("Tools/Add Mesh Colliders (chọn object cha trước)")]
    static void Add()
    {
        int added = 0, skipped = 0;
        foreach (GameObject root in Selection.gameObjects)
        {
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponent<Collider>() != null) { skipped++; continue; }

                MeshCollider mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                mc.sharedMesh = mf.sharedMesh;
                added++;
            }
        }
        Debug.Log($"[AddMeshColliders] Đã thêm {added} Mesh Collider, bỏ qua {skipped} object đã có collider.");
    }

    [MenuItem("Tools/Add Mesh Colliders (chọn object cha trước)", true)]
    static bool Validate() => Selection.gameObjects.Length > 0;

    [MenuItem("Tools/Remove Mesh Colliders (chọn object cha trước)")]
    static void Remove()
    {
        int removed = 0;
        foreach (GameObject root in Selection.gameObjects)
            foreach (MeshCollider mc in root.GetComponentsInChildren<MeshCollider>(true))
            {
                if (mc.isTrigger) continue; // giữ trigger
                Undo.DestroyObjectImmediate(mc);
                removed++;
            }
        Debug.Log($"[AddMeshColliders] Đã gỡ {removed} Mesh Collider.");
    }

    [MenuItem("Tools/Remove Mesh Colliders (chọn object cha trước)", true)]
    static bool ValidateRemove() => Selection.gameObjects.Length > 0;
}
