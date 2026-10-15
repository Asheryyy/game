using UnityEngine;
using UnityEditor;

/// <summary>
/// Tools → Đặt đồ cầm tay vào góc màn hình: chọn object cầm tay (vd: ChoiCamTay, con của Main Camera) rồi chạy.
/// Tự động: thu nhỏ model cho dài vừa tay, xoay cán chéo lên trên-trái, đặt ở góc dưới phải trước camera.
/// Chỉnh số trong cửa sổ hiện ra rồi bấm lại cho tới khi vừa ý. Ctrl+Z để hoàn tác.
/// </summary>
public class FitHeldTool : EditorWindow
{
    float length = 0.9f;                                    // chiều dài vật trên màn hình (m)
    Vector3 screenPos = new Vector3(0.32f, -0.38f, 0.65f);  // phải/trên/trước so với camera
    Vector3 pointTo = new Vector3(-0.35f, 1f, 0.3f);        // hướng cán: lên trên, hơi sang trái, hơi ra trước
    bool flip;                                              // lật đầu-đuôi (model xuất ngược chiều)

    [MenuItem("Tools/Đặt đồ cầm tay vào góc màn hình")]
    static void Open() => GetWindow<FitHeldTool>("Đồ cầm tay");

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Chọn object cầm tay (vd: ChoiCamTay, là con của Main Camera) rồi bấm Áp dụng.", MessageType.Info);
        length    = EditorGUILayout.Slider("Chiều dài (m)", length, 0.2f, 2f);
        screenPos = EditorGUILayout.Vector3Field("Vị trí (phải, trên, trước)", screenPos);
        pointTo   = EditorGUILayout.Vector3Field("Hướng cán", pointTo);
        flip      = EditorGUILayout.Toggle("Lật ngược đầu - đuôi", flip);
        GUI.enabled = Selection.activeTransform != null;
        if (GUILayout.Button("Áp dụng", GUILayout.Height(30))) Apply(Selection.activeTransform);
        GUI.enabled = true;
    }

    void Apply(Transform holder)
    {
        Camera cam = holder.GetComponentInParent<Camera>();
        if (cam == null) { EditorUtility.DisplayDialog("Đồ cầm tay", "Object này phải nằm trong Main Camera.", "OK"); return; }
        if (holder.childCount == 0) { EditorUtility.DisplayDialog("Đồ cầm tay", "Kéo model (vd: Broom_001) vào làm con của object này trước.", "OK"); return; }
        Transform model = holder.GetChild(0);

        Undo.RecordObjects(new Object[] { holder, model }, "Đặt đồ cầm tay");

        // 1. holder: đặt trước camera, không xoay, scale bù để 1 đơn vị local = 1 mét so với camera
        holder.localRotation = Quaternion.identity;
        holder.localScale = Vector3.one;
        Vector3 camScale = cam.transform.lossyScale;
        holder.localPosition = new Vector3(screenPos.x / camScale.x, screenPos.y / camScale.y, screenPos.z / camScale.z);
        holder.localScale = new Vector3(1f / camScale.x, 1f / camScale.y, 1f / camScale.z);

        // 2. tìm trục dài nhất của model (trong toạ độ của chính model)
        model.localRotation = Quaternion.identity;
        model.localPosition = Vector3.zero;
        model.localScale = Vector3.one;
        Bounds lb = LocalBounds(model, model);
        Vector3 s = lb.size;
        Vector3 axis = s.x >= s.y && s.x >= s.z ? Vector3.right : (s.y >= s.z ? Vector3.up : Vector3.forward);
        float longest = Mathf.Max(s.x, s.y, s.z);
        if (longest < 1e-5f) return;

        // 3. thu nhỏ cho đúng chiều dài, xoay trục dài theo hướng cán
        model.localScale = Vector3.one * (length / longest);
        model.localRotation = Quaternion.FromToRotation(flip ? -axis : axis, pointTo.normalized);

        // 4. dời cho tâm model trùng tâm holder
        Bounds hb = LocalBounds(model, holder);
        model.localPosition -= hb.center;

        EditorUtility.SetDirty(holder);
        EditorUtility.SetDirty(model);
        Debug.Log($"[Đồ cầm tay] Đã đặt {model.name}: dài {length}m, ở {screenPos} so với camera.");
    }

    // khung bao các mesh của 'root', tính trong toạ độ local của 'space'
    static Bounds LocalBounds(Transform root, Transform space)
    {
        bool has = false;
        Bounds b = new Bounds();
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            Bounds mb = mf.sharedMesh.bounds;
            Vector3 c = mb.center, e = mb.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = space.InverseTransformPoint(mf.transform.TransformPoint(corner));
                if (!has) { b = new Bounds(p, Vector3.zero); has = true; } else b.Encapsulate(p);
            }
        }
        return b;
    }
}
