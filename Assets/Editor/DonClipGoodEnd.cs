using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using System.Linq;

/// <summary>
/// Tools → Good End: dồn clip theo camera.
/// Sau khi rút/dời các shot camera: mọi clip KHÁC (phụ đề, màn đen, nhân vật, xe...) đang bắt đầu từ giờ cũ của shot XeCho
/// trở về sau được dời cùng một khoảng để khớp lại với camera. Viền đen kéo theo tổng thời lượng.
/// Chạy lại khi đã khớp thì không làm gì.
/// </summary>
public static class DonClipGoodEnd
{
    [MenuItem("Tools/Good End: dồn clip theo camera")]
    static void Run()
    {
        var director = XeDenPhaGoodEnd.Find("Cutscene_GoodEnd")?.GetComponent<PlayableDirector>();
        var timeline = director != null ? director.playableAsset as TimelineAsset : null;
        TimelineClip xeCho = timeline != null ? XeDenPhaGoodEnd.Shot(timeline, director, "Cam_GE_7_XeCho") : null;
        TrackAsset actTrack = timeline?.GetOutputTracks().FirstOrDefault(t => t.name == "Xe bat den");
        TimelineClip mark = actTrack?.GetClips().FirstOrDefault(); // đèn xe luôn bật cùng lúc với shot XeCho
        if (xeCho == null || mark == null) { Debug.LogError($"[DonClip] Thiếu shot XeCho={xeCho != null} hoặc track Xe bat den={mark != null}"); return; }

        double oldStart = mark.start, delta = xeCho.start - oldStart;
        if (System.Math.Abs(delta) < 0.001) { Debug.Log("[DonClip] Đã khớp, không cần dồn."); return; }

        int n = 0;
        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            if (track is Unity.Cinemachine.CinemachineTrack || track.name == "Vien den") continue;
            var clips = track.GetClips().Where(c => c.start >= oldStart - 0.35).ToList(); // phụ đề bắt đầu sớm hơn shot 0.2s
            if (clips.Count == 0) continue;
            Undo.RecordObject(track, "Don clip");
            foreach (var c in clips) { c.start += delta; n++; }
            EditorUtility.SetDirty(track);
        }

        // viền đen phủ hết cutscene
        double end = timeline.GetOutputTracks().OfType<Unity.Cinemachine.CinemachineTrack>().SelectMany(t => t.GetClips()).Max(c => c.end);
        var vien = timeline.GetOutputTracks().FirstOrDefault(t => t.name == "Vien den");
        var vc = vien?.GetClips().FirstOrDefault();
        if (vc != null) { Undo.RecordObject(vien, "Don clip"); vc.duration = end - vc.start; EditorUtility.SetDirty(vien); }

        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DonClip] Đã dời {n} clip {delta:+0.##;-0.##}s cho khớp camera. Cutscene dài {end:0.#}s.");
    }
}
