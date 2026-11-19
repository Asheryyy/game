using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Linq;

/// <summary>
/// Tools → Cutscene: Ghi góc Scene view thành key (Ctrl+Alt+K).
/// Chọn object (vd: Camera_Chayvenha) đang được 1 Animation Track trong Timeline điều khiển,
/// bay góc nhìn trong Scene view tới chỗ muốn, kéo thanh Timeline tới frame cần → bấm lệnh này.
/// Ghi vị trí + góc xoay vào clip Recorded của track đó (thay cho nút Record hay bị trượt).
/// </summary>
public static class GhiKeyCameraTimeline
{
    [MenuItem("Tools/Cutscene: Ghi góc Scene view thành key %&k")]
    static void Ghi()
    {
        GameObject go = Selection.activeGameObject;
        PlayableDirector dir = TimelineEditor.inspectedDirector;
        SceneView sv = SceneView.lastActiveSceneView;
        if (go == null || dir == null || sv == null)
        {
            Debug.LogError("[Cutscene] Cần: chọn object, mở Timeline của cutscene, và có cửa sổ Scene.");
            return;
        }
        var timeline = dir.playableAsset as TimelineAsset;
        AnimationTrack track = timeline.GetOutputTracks().OfType<AnimationTrack>().FirstOrDefault(t =>
        {
            Object b = dir.GetGenericBinding(t);
            return b == go || (b is Component c && c.gameObject == go);
        });
        if (track == null)
        {
            // hay bị chọn nhầm object chứa Timeline → tự lấy track đang gắn với camera
            track = timeline.GetOutputTracks().OfType<AnimationTrack>().FirstOrDefault(t =>
            {
                Object b = dir.GetGenericBinding(t);
                return b is Component c && c.GetComponent<Camera>() != null;
            });
            if (track == null) { Debug.LogError($"[Cutscene] Không có Animation Track nào gắn với {go.name} hay với camera."); return; }
            go = ((Component)dir.GetGenericBinding(track)).gameObject;
        }

        Undo.RegisterCompleteObjectUndo(timeline, "Ghi key camera");
        track.trackOffset = TrackOffset.ApplySceneOffsets; // key = đúng vị trí cục bộ, không bị offset cộng thêm
        if (track.infiniteClip == null) track.CreateInfiniteClip("Recorded");
        AnimationClip clip = track.infiniteClip;
        Undo.RegisterCompleteObjectUndo(clip, "Ghi key camera");

        Transform t = go.transform;
        Transform cam = sv.camera.transform;
        Vector3 p = t.parent ? t.parent.InverseTransformPoint(cam.position) : cam.position;
        Quaternion r = t.parent ? Quaternion.Inverse(t.parent.rotation) * cam.rotation : cam.rotation;
        float time = (float)dir.time;

        // góc xoay ghi bằng quaternion (Euler hay lật ngược khi camera nằm dưới object cha bị xoay)
        foreach (string ax in new[] { "x", "y", "z" })
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw." + ax), null);
        var qx = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.x"));
        if (qx != null && qx.length > 0)
        {
            int n = System.Array.FindIndex(qx.keys, k => k.time == qx.keys.OrderBy(kk => Mathf.Abs(kk.time - time)).First().time);
            Quaternion prev = new Quaternion(qx.keys[n].value, Curve(clip, "y").keys[n].value, Curve(clip, "z").keys[n].value, Curve(clip, "w").keys[n].value);
            if (Quaternion.Dot(prev, r) < 0f) r = new Quaternion(-r.x, -r.y, -r.z, -r.w); // cùng bán cầu → xoay đường ngắn nhất
        }

        SetKey(clip, "m_LocalPosition.x", time, p.x); SetKey(clip, "m_LocalPosition.y", time, p.y); SetKey(clip, "m_LocalPosition.z", time, p.z);
        SetKey(clip, "m_LocalRotation.x", time, r.x); SetKey(clip, "m_LocalRotation.y", time, r.y);
        SetKey(clip, "m_LocalRotation.z", time, r.z); SetKey(clip, "m_LocalRotation.w", time, r.w);

        Undo.RecordObject(t, "Ghi key camera");
        t.localPosition = p; t.localRotation = r;
        EditorUtility.SetDirty(clip); EditorUtility.SetDirty(timeline);
        TimelineEditor.Refresh(RefreshReason.ContentsModified);
        Debug.Log($"[Cutscene] Đã ghi key {go.name} tại frame {Mathf.RoundToInt(time * (float)timeline.editorSettings.frameRate)}", go);
    }

    static AnimationCurve Curve(AnimationClip clip, string ax) =>
        AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation." + ax));

    static void SetKey(AnimationClip clip, string prop, float time, float value)
    {
        var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), prop);
        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
        int i = System.Array.FindIndex(curve.keys, k => Mathf.Abs(k.time - time) < 0.0005f);
        if (i >= 0) curve.RemoveKey(i);
        curve.AddKey(new Keyframe(time, value));
        for (int k = 0; k < curve.length; k++) AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.ClampedAuto);
        for (int k = 0; k < curve.length; k++) AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.ClampedAuto);
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }
}
