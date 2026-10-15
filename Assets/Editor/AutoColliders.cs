using UnityEngine;
using UnityEditor;

/// <summary>
/// Tools → Tự động gắn Collider (chọn cả phòng): đi qua mọi vật trong object đang chọn và tự quyết:
///   - Vật nhỏ (< 40cm) hoặc ở trên cao (đèn trần, trần nhà...)  → bỏ qua, không collider
///   - Có chữ "door" trong tên (tường có cửa, cánh cửa)           → Mesh Collider (chừa lỗ cửa)
///   - Còn lại (tường, sàn, giường, tủ, sofa...)                   → Box Collider vừa khít (rất nhẹ)
///   - Bỏ qua nhân vật và các vật có script tương tác (manh mối, cửa, giường ngủ...)
/// Vật được gắn Box còn được đánh dấu Static để Unity gộp hình lại → nhẹ máy hơn.
/// Ctrl+Z để hoàn tác.
/// </summary>
public static class AutoColliders
{
    const float SmallSize = 0.4f;     // nhỏ hơn → bỏ qua
    const float ReachHeight = 2.2f;   // cao hơn sàn chừng này → người chơi không chạm tới

    [MenuItem("Tools/Tự động gắn Collider (chọn cả phòng)")]
    static void Run()
    {
        int box = 0, mesh = 0, skipped = 0;
        Undo.SetCurrentGroupName("Tự động gắn Collider");
        int group = Undo.GetCurrentGroup();

        foreach (GameObject root in Selection.gameObjects)
        {
            Renderer[] all = root.GetComponentsInChildren<Renderer>(true);
            if (all.Length == 0) continue;

            // sàn = điểm thấp nhất của cả phòng
            float floorY = float.MaxValue;
            foreach (Renderer r in all) floorY = Mathf.Min(floorY, r.bounds.min.y);

            foreach (Renderer r in all)
            {
                if (!(r is MeshRenderer) || r.GetComponent<MeshFilter>() == null) { skipped++; continue; }
                GameObject go = r.gameObject;

                if (ShouldSkip(go)) { skipped++; continue; }
                if (HasSolidCollider(go)) { skipped++; continue; }

                Bounds b = r.bounds;
                if (Mathf.Max(b.size.x, b.size.y, b.size.z) < SmallSize) { skipped++; continue; }
                if (b.min.y > floorY + ReachHeight) { skipped++; continue; }

                if (IsDoorPart(go.transform))
                {
                    MeshCollider mc = Undo.AddComponent<MeshCollider>(go);
                    mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                    mesh++;
                }
                else
                {
                    BoxCollider bc = Undo.AddComponent<BoxCollider>(go);
                    FitBox(bc, go.GetComponent<MeshFilter>().sharedMesh);
                    var flags = GameObjectUtility.GetStaticEditorFlags(go) | StaticEditorFlags.BatchingStatic;
                    GameObjectUtility.SetStaticEditorFlags(go, flags);
                    box++;
                }
            }
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log($"[AutoColliders] Box: {box}, Mesh (có cửa): {mesh}, bỏ qua: {skipped}");
    }

    [MenuItem("Tools/Tự động gắn Collider (chọn cả phòng)", true)]
    static bool Validate() => Selection.gameObjects.Length > 0;

    // Gỡ hết collider CHẶN (không phải trigger) trong object đang chọn — để làm lại từ đầu
    [MenuItem("Tools/Gỡ collider chặn (giữ trigger)")]
    static void RemoveSolid()
    {
        int n = 0;
        foreach (GameObject root in Selection.gameObjects)
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger || c is CharacterController || ShouldSkip(c.gameObject)) continue;
                Undo.DestroyObjectImmediate(c);
                n++;
            }
        Debug.Log($"[AutoColliders] Đã gỡ {n} collider chặn.");
    }

    [MenuItem("Tools/Gỡ collider chặn (giữ trigger)", true)]
    static bool ValidateRemove() => Selection.gameObjects.Length > 0;

    // Bỏ qua nhân vật và vật có script tương tác (dao, chìa khóa, giường, cửa...)
    static bool ShouldSkip(GameObject go)
    {
        if (go.GetComponentInParent<Animator>(true) != null) return true;
        if (go.GetComponentInParent<CharacterController>(true) != null) return true;
        foreach (MonoBehaviour mb in go.GetComponentsInParent<MonoBehaviour>(true))
            if (mb != null && string.IsNullOrEmpty(mb.GetType().Namespace)) return true; // script của game (không có namespace)
        return false;
    }

    // object hoặc cha của nó có chữ "door" (vd: wall_with_small_door/wall) → cần Mesh Collider để chừa lỗ cửa
    static bool IsDoorPart(Transform t)
    {
        for (int i = 0; t != null && i < 3; t = t.parent, i++)
            if (t.name.ToLower().Contains("door")) return true;
        return false;
    }

    static bool HasSolidCollider(GameObject go)
    {
        foreach (Collider c in go.GetComponents<Collider>())
            if (!c.isTrigger) return true;
        return false;
    }

    // Box vừa khít theo hình gốc của mesh (toạ độ local nên xoay/scale vẫn đúng)
    static void FitBox(BoxCollider bc, Mesh m)
    {
        if (m == null) return;
        bc.center = m.bounds.center;
        bc.size = m.bounds.size;
    }
}
