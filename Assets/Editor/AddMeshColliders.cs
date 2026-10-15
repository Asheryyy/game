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

    // Đồ đạc (giường, tủ, sofa...): 1 Box Collider bao trọn vật, nhẹ hơn Mesh Collider nhiều
    [MenuItem("Tools/Add Box Collider bao quanh (mỗi object đang chọn)")]
    static void AddBox()
    {
        int added = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) continue;
            Bounds b = rends[0].bounds;
            foreach (Renderer r in rends) b.Encapsulate(r.bounds);

            BoxCollider box = go.GetComponent<BoxCollider>();
            if (box == null) box = Undo.AddComponent<BoxCollider>(go);
            else Undo.RecordObject(box, "Box Collider");

            // đổi khung bao thế giới → toạ độ local của object
            box.center = go.transform.InverseTransformPoint(b.center);
            Vector3 s = go.transform.lossyScale;
            box.size = new Vector3(b.size.x / Mathf.Abs(s.x), b.size.y / Mathf.Abs(s.y), b.size.z / Mathf.Abs(s.z));
            box.isTrigger = false;
            added++;
        }
        Debug.Log($"[Colliders] Đã gắn Box Collider cho {added} object.");
    }

    [MenuItem("Tools/Add Box Collider bao quanh (mỗi object đang chọn)", true)]
    static bool ValidateBox() => Selection.gameObjects.Length > 0;

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
