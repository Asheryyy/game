using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using Unity.Cinemachine;
using System.Linq;

/// <summary>
/// Tools → Good End: xe đón bằng đèn pha (không cần model xe).
/// GE_Xe_Root/GE_Xe = 2 đèn pha (chiếu về phía người chơi) + 2 đèn hậu đỏ.
///   - bật từ shot XeCho (nhân vật ra khỏi cửa đã thấy đèn) tới hết shot XeChayDi
///   - shot XeChayDi: xe quay đầu rồi chạy xa → thấy đèn hậu đỏ nhỏ dần
/// Chỗ đỗ xe = GE_XeDon: menu "đặt xe đón tại chỗ đang nhìn" đặt nó ở giữa Scene View (sân chơi),
/// đèn pha chiếu về phía người nhìn. Tự kéo/xoay GE_XeDon (trục Z xanh = hướng đèn) rồi chạy menu "xe đón bằng đèn pha".
/// </summary>
public static class XeDenPhaGoodEnd
{
    const float QuayDauGiay = 1.5f;   // thời gian quay đầu xe
    const float QuangDuong = 25f;     // chạy xa bao nhiêu mét trong shot XeChayDi
    const float LuiTocDo = 4f;        // tốc độ lùi lúc quay đầu (m/s) → lùi ra khoảng 4m

    /// Mở Scene View nhìn vào sân chơi → chạy menu này: xe đỗ đúng điểm giữa màn hình, đèn chiếu về phía bạn đang nhìn.
    [MenuItem("Tools/Good End: đặt xe đón tại chỗ đang nhìn (Scene View)")]
    static void PlaceAtView() => Build(true);

    /// Dựng lại xe theo GE_XeDon hiện có (sau khi tự kéo/xoay GE_XeDon)
    [MenuItem("Tools/Good End: xe đón bằng đèn pha")]
    static void Rebuild() => Build(false);

    // tia từ giữa Scene View xuống mặt đất
    static bool ViewHit(out Vector3 point, out Vector3 toViewer)
    {
        point = default; toViewer = Vector3.forward;
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return false;
        Transform c = sv.camera.transform;
        Vector3 flat = Vector3.ProjectOnPlane(-c.forward, Vector3.up);
        if (flat.sqrMagnitude > 0.01f) toViewer = flat.normalized;
        if (Physics.Raycast(c.position, c.forward, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore)) { point = hit.point; return true; }
        point = sv.pivot; // không trúng collider → dùng tâm xoay của Scene View
        return true;
    }

    static void Build(bool placeAtView)
    {
        Transform cutRoot = Find("Cutscene_GoodEnd");
        var director = cutRoot != null ? cutRoot.GetComponent<PlayableDirector>() : null;
        var timeline = director != null ? director.playableAsset as TimelineAsset : null;
        if (timeline == null) { Debug.LogError("[XeDon] Không thấy Cutscene_GoodEnd / Timeline"); return; }
        // đèn xe bật từ shot xe chờ (shot cầu thang đã bỏ)
        TimelineClip cauThang = Shot(timeline, director, "Cam_GE_7_XeCho");
        TimelineClip xeChayDi = Shot(timeline, director, "Cam_GE_8_XeChayDi");
        if (cauThang == null || xeChayDi == null) { Debug.LogError($"[XeDon] Thiếu shot trên Camera (shot): XeCho={cauThang != null} XeChayDi={xeChayDi != null}"); return; }

        // ── chỗ đỗ xe ──
        Transform don = Find("GE_XeDon");
        if (don == null || placeAtView)
        {
            if (!ViewHit(out Vector3 p, out Vector3 toViewer))
            {
                Debug.LogError("[XeDon] Không xác định được chỗ đặt xe: mở Scene View, nhìn vào sân chơi (giữa màn hình phải trúng mặt đất) rồi chạy lại.");
                return;
            }
            if (don == null)
            {
                var go = new GameObject("GE_XeDon");
                Undo.RegisterCreatedObjectUndo(go, "Xe den pha");
                go.transform.SetParent(cutRoot, true);
                don = go.transform;
            }
            Undo.RecordObject(don, "Xe den pha");
            don.SetPositionAndRotation(p, Quaternion.LookRotation(toViewer)); // đèn pha chiếu về phía người đang nhìn
        }

        // ── dựng lại xe đèn ──
        Transform oldRoot = Find("GE_Xe_Root");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot.gameObject);
        var root = new GameObject("GE_Xe_Root");
        Undo.RegisterCreatedObjectUndo(root, "Xe den pha");
        root.transform.SetParent(cutRoot, false);
        var xe = new GameObject("GE_Xe");
        xe.transform.SetParent(root.transform, false);
        xe.transform.SetPositionAndRotation(don.position, Quaternion.Euler(0f, don.eulerAngles.y, 0f));

        // thân xe: khối tối, ban đêm chỉ là bóng đen nhưng che được đèn phía bên kia (nhìn trước không thấy đèn hậu)
        GameObject than = GameObject.CreatePrimitive(PrimitiveType.Cube);
        than.name = "ThanXe";
        Object.DestroyImmediate(than.GetComponent<Collider>());
        than.transform.SetParent(xe.transform, false);
        than.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        than.transform.localScale = new Vector3(1.8f, 1.1f, 4.1f);
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null)
        {
            var m = new Material(lit) { name = "ThanXe" };
            m.SetColor("_BaseColor", new Color(0.04f, 0.04f, 0.045f));
            m.SetFloat("_Smoothness", 0.7f); // sơn bóng, hắt chút ánh đèn
            than.GetComponent<MeshRenderer>().sharedMaterial = m;
        }

        foreach (float side in new[] { -0.75f, 0.75f })
        {
            Light pha = NewLight(xe.transform, side < 0 ? "DenPha_Trai" : "DenPha_Phai", LightType.Spot,
                new Vector3(side, 0.7f, 2.1f), Quaternion.Euler(4f, 0f, 0f));
            pha.color = new Color(1f, 0.95f, 0.8f);
            pha.intensity = 25f; pha.range = 30f; pha.spotAngle = 55f; pha.innerSpotAngle = 25f;
            pha.shadows = LightShadows.Soft;

            Light hau = NewLight(xe.transform, side < 0 ? "DenHau_Trai" : "DenHau_Phai", LightType.Point,
                new Vector3(side, 0.8f, -2.1f), Quaternion.identity);
            hau.color = new Color(1f, 0.05f, 0.02f);
            hau.intensity = 3f; hau.range = 2.5f;

            // Light của Unity không tự thấy bóng đèn → thêm bóng phát sáng (Bloom làm nó chói như đèn thật)
            Bulb(pha.transform, "BongPha", new Vector3(0.28f, 0.16f, 0.08f), new Color(1f, 0.95f, 0.8f) * 6f);
            Bulb(hau.transform, "BongHau", new Vector3(0.22f, 0.1f, 0.06f), new Color(1f, 0.05f, 0.02f) * 4f);
        }

        // ── xe chạy đi: quay đầu rồi tăng tốc ──
        float t0 = (float)xeChayDi.start, dur = (float)xeChayDi.duration;
        string[] props = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
        var curves = props.Select(_ => new AnimationCurve()).ToArray();
        Vector3 pos = xe.transform.localPosition;
        float yaw0 = xe.transform.localEulerAngles.y, quay = Mathf.Min(QuayDauGiay, dur * 0.5f);
        float vMax = QuangDuong / Mathf.Max(0.1f, dur - quay - 0.4f);
        const float dt = 1f / 30f;
        for (float t = 0f; ; t = Mathf.Min(t + dt, dur))
        {
            float k = Mathf.Clamp01(t / quay);
            float yaw = yaw0 + 180f * Mathf.SmoothStep(0f, 1f, k);
            // đầu xe đang hướng vào nhà → vừa LÙI vừa quay đầu (đuôi xe vòng ra sân), quay xong mới tăng tốc chạy đi
            float v = t <= quay ? -LuiTocDo * Mathf.Sin(Mathf.PI * k) : vMax * Mathf.Clamp01((t - quay) / 0.8f);
            if (t > 0f) pos += Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * v * dt;
            Quaternion r = Quaternion.Euler(0f, yaw, 0f);
            float[] val = { pos.x, pos.y, pos.z, r.x, r.y, r.z, r.w };
            for (int i = 0; i < val.Length; i++) curves[i].AddKey(t0 + t, val[i]);
            if (t >= dur) break;
        }

        // xóa track/clip cũ của lần chạy trước
        string tlPath = AssetDatabase.GetAssetPath(timeline);
        foreach (var old in timeline.GetOutputTracks().Where(t => t.name == "Xe chay di" || t.name == "Xe bat den").ToList()) timeline.DeleteTrack(old);
        foreach (var old in AssetDatabase.LoadAllAssetsAtPath(tlPath).OfType<AnimationClip>().Where(c => c.name == "XeChayDi"))
        { AssetDatabase.RemoveObjectFromAsset(old); Object.DestroyImmediate(old, true); }

        // clip bắt đầu từ 0 (key trước t0 giữ nguyên vị trí đỗ) → đặt clip đúng khoảng xe xuất hiện
        var clip = new AnimationClip { name = "XeChayDi", frameRate = 30 };
        float show0 = (float)cauThang.start;
        for (int i = 0; i < props.Length; i++)
        {
            var c = curves[i];
            c.AddKey(show0, c.keys[0].value); // đứng yên từ lúc hiện tới lúc chạy
            clip.SetCurve("GE_Xe", typeof(Transform), props[i], c);
        }
        clip.EnsureQuaternionContinuity();
        AssetDatabase.AddObjectToAsset(clip, timeline);

        var animator = Undo.AddComponent<Animator>(root);
        var move = timeline.CreateTrack<AnimationTrack>(null, "Xe chay di");
        var mc = move.CreateClip(clip);
        mc.start = show0; mc.duration = xeChayDi.end - show0;
        mc.clipIn = show0; // key trong clip tính theo giờ Timeline
        director.SetGenericBinding(move, animator);

        var act = timeline.CreateTrack<ActivationTrack>(null, "Xe bat den");
        var ac = act.CreateDefaultClip();
        ac.start = show0; ac.duration = xeChayDi.end - show0;
        director.SetGenericBinding(act, xe);
        xe.SetActive(false);

        // Show During: chỉ hiện lúc chiếu cutscene
        var player = cutRoot.GetComponent<CutscenePlayer>();
        var so = new SerializedObject(player);
        var show = so.FindProperty("showDuring");
        bool has = false;
        for (int i = 0; i < show.arraySize; i++)
        {
            var o = show.GetArrayElementAtIndex(i).objectReferenceValue;
            if (o == root) has = true;
        }
        if (!has) { show.arraySize++; show.GetArrayElementAtIndex(show.arraySize - 1).objectReferenceValue = root; }
        // dọn ô trống (GE_Xe_Root cũ bị xóa)
        for (int i = show.arraySize - 1; i >= 0; i--)
            if (show.GetArrayElementAtIndex(i).objectReferenceValue == null) show.DeleteArrayElementAtIndex(i);
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(director);
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(cutRoot.gameObject.scene);
        Selection.activeGameObject = don.gameObject;
        Debug.Log($"[XeDon] Đã dựng xe đèn pha: hiện {show0:0.#}s → {xeChayDi.end:0.#}s, chạy đi từ {t0:0.#}s. " +
                  "Kéo/xoay GE_XeDon (trục Z xanh = hướng đèn) rồi chạy lại menu.", don);
    }

    // shortcut: material tạo trong scene (không ra file asset); muốn chỉnh chung thì tạo .mat riêng
    static void Bulb(Transform parent, string name, Vector3 size, Color hdr)
    {
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        b.name = name;
        Object.DestroyImmediate(b.GetComponent<Collider>());
        b.transform.SetParent(parent, false);
        b.transform.localScale = size;
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(sh != null ? sh : Shader.Find("Unlit/Color")) { name = name };
        mat.SetColor(sh != null ? "_BaseColor" : "_Color", hdr);
        var r = b.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Light NewLight(Transform parent, string name, LightType type, Vector3 localPos, Quaternion localRot)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetLocalPositionAndRotation(localPos, localRot);
        var l = go.AddComponent<Light>();
        l.type = type;
        return l;
    }

    // ô shot trên track Camera (shot) đang dùng camera tên camName
    internal static TimelineClip Shot(TimelineAsset tl, PlayableDirector d, string camName) =>
        tl.GetOutputTracks().OfType<CinemachineTrack>().SelectMany(t => t.GetClips())
          .FirstOrDefault(c => c.asset is CinemachineShot s && s.VirtualCamera.Resolve(d) is Component v && v != null && v.name == camName);

    internal static Transform Find(string name) =>
        Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
              .FirstOrDefault(t => t.name == name && !(t is RectTransform));
}
