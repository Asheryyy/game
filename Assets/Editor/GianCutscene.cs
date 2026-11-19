using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tools → Giãn thời lượng cutscene: chọn object cutscene (có PlayableDirector), gõ số giây muốn dài → Giãn.
/// Giãn đều MỌI track: clip, shot camera, phụ đề, màn đen, keyframe di chuyển (Recorded).
/// Clip animation chậm lại cùng tỉ lệ (chân khớp đường đi). Âm thanh KHÔNG trầm giọng:
/// clip lặp (bước chân) kéo dài ra, clip 1 lần (tiếng đánh) chỉ dời vị trí. Ctrl+Z để hoàn tác.
/// </summary>
public class GianCutscene : EditorWindow
{
    float target = 20f;

    [MenuItem("Tools/Giãn thời lượng cutscene")]
    static void Open() => GetWindow<GianCutscene>("Giãn cutscene");

    void OnSelectionChange() => Repaint();

    void OnGUI()
    {
        GameObject go = Selection.activeGameObject;
        PlayableDirector dir = go != null ? go.GetComponent<PlayableDirector>() : null;
        TimelineAsset tl = dir != null ? dir.playableAsset as TimelineAsset : null;
        if (tl == null)
        {
            EditorGUILayout.HelpBox("Chọn object cutscene (vd: Cutscene_BadEnd1) trong Hierarchy.", MessageType.Info);
            return;
        }
        EditorGUILayout.LabelField("Timeline", tl.name);
        EditorGUILayout.LabelField("Đang dài", $"{tl.duration:0.00} giây");
        target = EditorGUILayout.FloatField("Muốn dài (giây)", target);
        if (GUILayout.Button("Giãn", GUILayout.Height(30)) && target > 0.1f && tl.duration > 0.01)
        {
            double k = target / tl.duration;
            Stretch(tl, k);
            Debug.Log($"[Giãn cutscene] {tl.name}: x{k:0.00} → {tl.duration:0.00} giây");
        }
    }

    static void Stretch(TimelineAsset tl, double k)
    {
        List<TrackAsset> tracks = tl.GetOutputTracks().ToList(); // gồm cả track trong group (Phụ đề)
        var undo = new List<Object> { tl };
        undo.AddRange(tracks);
        foreach (TrackAsset t in tracks)
        {
            foreach (TimelineClip c in t.GetClips()) if (c.asset != null) undo.Add(c.asset);
            if (t is AnimationTrack a && a.infiniteClip != null) undo.Add(a.infiniteClip);
        }
        Undo.RegisterCompleteObjectUndo(undo.ToArray(), "Giãn cutscene");

        foreach (TrackAsset t in tracks)
        {
            // từ clip cuối lên đầu → kéo dài clip trước không đè lên clip sau chưa dời
            foreach (TimelineClip c in t.GetClips().OrderByDescending(c => c.start))
            {
                bool oneShot = c.asset is AudioPlayableAsset au && !au.loop;
                c.start *= k;
                if (!oneShot) c.duration *= k;
                if (t is AnimationTrack) c.timeScale /= k; // chân chậm lại cùng tỉ lệ với đường đi
            }
            if (t is AnimationTrack at && at.infiniteClip != null) ScaleKeys(at.infiniteClip, k);
            EditorUtility.SetDirty(t);
        }
        EditorUtility.SetDirty(tl);
        AssetDatabase.SaveAssets();
    }

    // keyframe Recorded (vị trí NhomChay_Root, camera...): giãn thời gian, giữ nguyên hình dạng đường cong
    static void ScaleKeys(AnimationClip clip, double k)
    {
        float f = (float)k;
        foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(clip))
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].time *= f;
                keys[i].inTangent /= f;
                keys[i].outTangent /= f;
            }
            curve.keys = keys;
            AnimationUtility.SetEditorCurve(clip, b, curve);
        }
    }
}
