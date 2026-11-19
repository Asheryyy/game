using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Tools → Sửa phụ đề BadEnd1: đổi chữ phụ đề ngay trong Editor (không đụng file scene khi Unity đang mở).
/// Chạy xong thì Ctrl+S. Có thể xóa file này sau khi dùng.
/// </summary>
public static class SuaPhuDeBadEnd1
{
    static readonly (string obj, string text)[] Moi =
    {
        ("PhuDe_4", "<b>Tôi:</b> Anh ơi! Trên phòng 302 có người bị bắt cóc! Anh gọi công an giúp em với!"),
        ("PhuDe_6", "<b>Tôi:</b> Camera? Tức là nãy giờ anh chứng kiến tất cả rồi à, thế tại sao anh không báo công an?"),
    };

    [MenuItem("Tools/Sửa phụ đề BadEnd1")]
    static void Sua()
    {
        var all = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var (obj, text) in Moi)
        {
            var t = System.Array.Find(all, x => x.name == obj && x.GetComponentInParent<Canvas>(true)?.name == "UI_BadEnd1");
            if (t == null) { Debug.LogError($"[PhuDe] Không thấy {obj} trong UI_BadEnd1"); continue; }
            Undo.RecordObject(t, "Sửa phụ đề");
            t.text = text;
            EditorUtility.SetDirty(t);
            Debug.Log($"[PhuDe] {obj}: {text}", t);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
