using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using Unity.Cinemachine;
using TMPro;
using System.Linq;

/// <summary>
/// Tools → Good End: bỏ cảnh công an (kể bằng chữ trên màn đen).
/// Xóa shot Cam_GE_10/11/12 (+ object camera, cả Cam_GE_6 cầu thang còn sót). Màn đen "Sáng hôm sau…" kéo dài tới hết,
/// PhuDe_10 đổi thành câu kể chuyện công an tóm gọn băng nhóm, rồi vào màn GOOD END.
/// </summary>
public static class BoCanhCongAnGoodEnd
{
    const string KeChuyen = "Công an ập vào tòa B, tóm gọn cả băng nhóm.";
    const float GiuMoiDong = 2.6f; // giây cho dòng kể chuyện

    [MenuItem("Tools/Good End: bỏ cảnh công an (kể bằng chữ trên màn đen)")]
    static void Run()
    {
        var director = XeDenPhaGoodEnd.Find("Cutscene_GoodEnd")?.GetComponent<PlayableDirector>();
        var timeline = director != null ? director.playableAsset as TimelineAsset : null;
        TimelineClip sang = timeline != null ? XeDenPhaGoodEnd.Shot(timeline, director, "Cam_GE_9_DenMan_Sang") : null;
        if (sang == null) { Debug.LogError("[BoCongAn] Không thấy shot Cam_GE_9_DenMan_Sang trên Camera (shot)"); return; }

        Undo.RecordObject(timeline, "Bo canh cong an");
        // 1. xóa shot camera công an + màn đen cuối
        foreach (string cam in new[] { "Cam_GE_10_CongAnToi", "Cam_GE_11_TomGon", "Cam_GE_12_DenMan" })
        {
            TimelineClip c = XeDenPhaGoodEnd.Shot(timeline, director, cam);
            if (c != null) timeline.DeleteClip(c);
        }

        // 2. màn đen "Sáng hôm sau…" giữ tới hết: dòng 9 → dòng kể chuyện
        TrackAsset pd9 = Track(timeline, "PhuDe_9"), pd10 = Track(timeline, "PhuDe_10");
        TimelineClip c9 = pd9?.GetClips().FirstOrDefault();
        double keStart = (c9 != null ? c9.end : sang.start + 2.2) + 0.2;
        double end = keStart + GiuMoiDong + 0.4;
        sang.duration = end - sang.start;

        TimelineClip c10 = pd10?.GetClips().FirstOrDefault();
        if (c10 != null)
        {
            c10.start = keStart; c10.duration = GiuMoiDong;
            if (director.GetGenericBinding(pd10) is GameObject go && go.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI tmp)
            {
                Undo.RecordObject(tmp, "Bo canh cong an");
                tmp.text = KeChuyen;
                EditorUtility.SetDirty(tmp);
            }
        }

        TrackAsset den = Track(timeline, "Man den");
        if (den != null)
        {
            var clips = den.GetClips().OrderBy(c => c.start).ToList();
            foreach (var c in clips.Where(c => c.start > sang.start + 0.01)) timeline.DeleteClip(c);
            TimelineClip first = clips.FirstOrDefault(c => c.start <= sang.start + 0.01 && c.end > sang.start);
            if (first != null) first.duration = end - first.start;
        }

        TimelineClip vien = Track(timeline, "Vien den")?.GetClips().FirstOrDefault();
        if (vien != null) vien.duration = end - vien.start;

        // 3. xóa object camera không còn dùng
        foreach (string cam in new[] { "Cam_GE_6_CauThang", "Cam_GE_10_CongAnToi", "Cam_GE_11_TomGon", "Cam_GE_12_DenMan" })
        {
            Transform t = XeDenPhaGoodEnd.Find(cam);
            if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
        }

        foreach (var t in timeline.GetOutputTracks()) EditorUtility.SetDirty(t);
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        Debug.Log($"[BoCongAn] Đã bỏ cảnh công an. Màn đen {sang.start:0.#}s → {end:0.#}s: \"Sáng hôm sau…\" rồi \"{KeChuyen}\". Cutscene dài {end:0.#}s.");
    }

    static TrackAsset Track(TimelineAsset tl, string name) => tl.GetOutputTracks().FirstOrDefault(t => t.name == name);
}
