using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu Tools → Lights: liệt kê / tắt bóng đổ của Point & Spot Light trong object đang chọn.
/// Point Light đổ bóng tốn 6 shadow map mỗi cái → nhiều đèn là lag nặng.
/// Bấm Ctrl+Z để hoàn tác.
/// </summary>
public static class LightShadowTools
{
    [MenuItem("Tools/Lights/Liệt kê đèn đang đổ bóng (cả scene)")]
    static void ListShadowLights()
    {
        int count = 0, maps = 0;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional || l.shadows == LightShadows.None) continue;
            count++;
            maps += l.type == LightType.Point ? 6 : 1;
            Debug.Log($"[Lights] {GetPath(l.transform)} ({l.type}, {l.shadows})", l);
        }
        Debug.Log($"[Lights] Tổng: {count} đèn đổ bóng ≈ {maps} shadow map. Nên giữ dưới ~4 đèn.");
    }

    [MenuItem("Tools/Lights/Tắt bóng đổ đèn trong object đang chọn")]
    static void DisableShadows()
    {
        int changed = 0;
        foreach (GameObject root in Selection.gameObjects)
            foreach (Light l in root.GetComponentsInChildren<Light>(true))
            {
                if (l.type == LightType.Directional || l.shadows == LightShadows.None) continue;
                Undo.RecordObject(l, "Tắt bóng đổ");
                l.shadows = LightShadows.None;
                changed++;
            }
        Debug.Log($"[Lights] Đã tắt bóng đổ {changed} đèn.");
    }

    [MenuItem("Tools/Lights/Tắt bóng đổ đèn trong object đang chọn", true)]
    static bool Validate() => Selection.gameObjects.Length > 0;

    // Đèn Baked chỉ sáng khi đã bake lightmap → copy từ scene khác sang là tối thui
    [MenuItem("Tools/Lights/Đổi đèn Baked sang Realtime trong object đang chọn")]
    static void MakeRealtime()
    {
        int changed = 0;
        foreach (GameObject root in Selection.gameObjects)
            foreach (Light l in root.GetComponentsInChildren<Light>(true))
            {
                if (l.lightmapBakeType == LightmapBakeType.Realtime) continue;
                Undo.RecordObject(l, "Đổi đèn sang Realtime");
                l.lightmapBakeType = LightmapBakeType.Realtime;
                changed++;
            }
        Debug.Log($"[Lights] Đã đổi {changed} đèn sang Realtime.");
    }

    [MenuItem("Tools/Lights/Đổi đèn Baked sang Realtime trong object đang chọn", true)]
    static bool ValidateRealtime() => Selection.gameObjects.Length > 0;

    static string GetPath(Transform t) => t.parent == null ? t.name : GetPath(t.parent) + "/" + t.name;
}
