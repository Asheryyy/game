using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using Unity.Cinemachine;
using System.Linq;

/// <summary>
/// Tools → Good End: chạy hành lang góc nhìn thứ nhất.
/// Shot "ChayHanhLang" của Cutscene_GoodEnd: Cam_GE_5 chạy từ ChayPOV_Dau → ChayPOV_Cuoi,
/// nhún theo bước chân + lắc người + rung tay (Cinemachine Noise). Không cần diễn viên.
/// Lần đầu: 2 điểm được tạo sẵn (đầu = chỗ camera, cuối = 12m phía trước) → kéo 2 điểm cho đúng hành lang
/// rồi chạy lại menu (mỗi lần chạy dựng lại track "Chay POV").
/// </summary>
public static class ChayPOVGoodEnd
{
    const float BuocChan = 0.33f;   // giây / 1 bước
    const float Nhun = 0.07f;       // nhún lên xuống (m)
    const float Lac = 0.04f;        // lắc ngang (m)
    const float CuiDau = 6f;        // hơi cúi khi chạy (độ)

    [MenuItem("Tools/Good End: chạy hành lang góc nhìn thứ nhất")]
    static void Build()
    {
        var director = Find("Cutscene_GoodEnd")?.GetComponent<PlayableDirector>();
        var timeline = director != null ? director.playableAsset as TimelineAsset : null;
        Transform cam = Find("Cam_GE_5_ChayHanhLang");
        if (timeline == null || cam == null) { Debug.LogError($"[ChayPOV] Thiếu Cutscene_GoodEnd/Timeline={timeline != null} hoặc Cam_GE_5_ChayHanhLang={cam != null}"); return; }
        // ô shot đang dùng Cam_GE_5 → lấy giờ bắt đầu/thời lượng (tìm theo camera, tên clip có thể đã bị đổi)
        TimelineClip shot = timeline.GetOutputTracks().OfType<CinemachineTrack>().SelectMany(t => t.GetClips())
            .FirstOrDefault(c => c.asset is CinemachineShot s && s.VirtualCamera.Resolve(director) is Component v && v != null && v.transform == cam);
        if (shot == null)
        {
            Debug.LogError("[ChayPOV] Trên track Camera (shot) không có ô nào dùng Cam_GE_5_ChayHanhLang. " +
                           "Chọn ô shot chạy hành lang → Inspector → kéo Cam_GE_5_ChayHanhLang vào ô Cinemachine Camera.");
            return;
        }

        Transform parent = cam.parent;
        Transform dau = Find("ChayPOV_Dau") ?? Marker("ChayPOV_Dau", cam.position, parent);
        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        Transform cuoi = Find("ChayPOV_Cuoi") ?? Marker("ChayPOV_Cuoi", cam.position + fwd.normalized * 12f, parent);

        // ── rung tay ──
        var noise = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise == null) noise = Undo.AddComponent<CinemachineBasicMultiChannelPerlin>(cam.gameObject);
        noise.NoiseProfile = AssetDatabase.LoadAssetAtPath<NoiseSettings>("Packages/com.unity.cinemachine/Presets/Noise/Handheld_normal_mild.asset");
        noise.AmplitudeGain = 1f;
        noise.FrequencyGain = 2f;
        EditorUtility.SetDirty(noise);

        // ── clip chạy: đi thẳng đầu → cuối + nhún/lắc theo bước chân ──
        string timelinePath = AssetDatabase.GetAssetPath(timeline);
        foreach (var old in timeline.GetOutputTracks().Where(t => t.name == "Chay POV").ToList()) timeline.DeleteTrack(old);
        foreach (var old in AssetDatabase.LoadAllAssetsAtPath(timelinePath).OfType<AnimationClip>().Where(c => c.name == "ChayPOV"))
        { AssetDatabase.RemoveObjectFromAsset(old); Object.DestroyImmediate(old, true); }

        Vector3 a = dau.position, b = cuoi.position;
        Vector3 flat = Vector3.ProjectOnPlane(b - a, Vector3.up);
        Quaternion look = Quaternion.LookRotation(flat.sqrMagnitude > 0.01f ? flat : fwd, Vector3.up) * Quaternion.Euler(CuiDau, 0f, 0f);
        float dur = (float)shot.duration;
        string[] props = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
        var curves = props.Select(_ => new AnimationCurve()).ToArray();
        for (float t = 0f; ; t = Mathf.Min(t + 1f / 30f, dur))
        {
            float p = t / BuocChan * Mathf.PI;
            Vector3 wp = Vector3.Lerp(a, b, t / dur)
                       + Vector3.up * (Mathf.Abs(Mathf.Sin(p)) * Nhun - Nhun * 0.5f)   // chạm đất mỗi bước
                       + look * Vector3.right * (Mathf.Sin(p * 0.5f) * Lac);           // dồn trọng tâm trái/phải
            Quaternion wr = look * Quaternion.Euler(Mathf.Sin(p) * 1.5f, 0f, Mathf.Sin(p * 0.5f) * 2.5f);
            Vector3 lp = parent != null ? parent.InverseTransformPoint(wp) : wp;
            Quaternion lr = parent != null ? Quaternion.Inverse(parent.rotation) * wr : wr;
            float[] v = { lp.x, lp.y, lp.z, lr.x, lr.y, lr.z, lr.w };
            for (int i = 0; i < v.Length; i++) curves[i].AddKey(t, v[i]);
            if (t >= dur) break;
        }
        var clip = new AnimationClip { name = "ChayPOV", frameRate = 30 };
        string path = parent != null ? AnimationUtility.CalculateTransformPath(cam, parent) : "";
        for (int i = 0; i < props.Length; i++) clip.SetCurve(path, typeof(Transform), props[i], curves[i]);
        clip.EnsureQuaternionContinuity();
        AssetDatabase.AddObjectToAsset(clip, timeline);

        // Animator đặt ở cha (Cams_GoodEnd) để camera là object con → không dính offset root motion của Timeline
        GameObject animRoot = parent != null ? parent.gameObject : cam.gameObject;
        var animator = animRoot.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(animRoot);
        var track = timeline.CreateTrack<AnimationTrack>(null, "Chay POV");
        var tc = track.CreateClip(clip);
        tc.start = shot.start; tc.duration = shot.duration;
        director.SetGenericBinding(track, animator);

        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(director);
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        Selection.objects = new Object[] { dau.gameObject, cuoi.gameObject };
        Debug.Log($"[ChayPOV] Đã dựng shot chạy ({shot.start:0.#}s → {shot.end:0.#}s, {Vector3.Distance(a, b):0.#}m). " +
                  "Kéo ChayPOV_Dau / ChayPOV_Cuoi cho đúng hành lang rồi chạy lại menu.", cam);
    }

    static Transform Marker(string name, Vector3 pos, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Chay POV");
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        return go.transform;
    }

    static Transform Find(string name) =>
        Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
              .FirstOrDefault(t => t.name == name && !(t is RectTransform));
}
