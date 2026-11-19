using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using Unity.Cinemachine;
using TMPro;

/// <summary>
/// Tools → Dựng khung Cutscene BadEnd1 (bảo vệ là đồng bọn).
/// Tạo: Cutscene_BadEnd1 (PlayableDirector + CutscenePlayer) + Timeline 8 shot,
/// camera cutscene có CinemachineBrain, 8 CinemachineCamera (đặt tại góc nhìn Scene hiện tại),
/// Canvas viền đen + phụ đề + màn đen (shot tên "DenMan..."), các Activation/Animation/Audio track gắn sẵn.
/// Việc còn lại: đặt góc từng camera, kéo diễn viên vào các track Animation, kéo âm thanh.
/// Build() dùng chung cho cutscene khác (vd: DungGoodEndTinTuong).
/// </summary>
public static class DungCutsceneBadEnd1
{
    // (tên shot, thời lượng giây, người nói, phụ đề)
    static readonly (string shot, float dur, string who, string line)[] Shots =
    {
        ("CauThang",  3.0f, "",          ""),
        ("BamLung",   3.0f, "Tôi",       "Cố lên… xuống tới phòng bảo vệ là an toàn rồi!"),
        ("PhongBaoVe",3.0f, "",          ""),
        ("CanMatToi", 3.5f, "Tôi",       "Chú ơi! Trên phòng 302 có người bị bắt cóc! Chú gọi công an giúp con với!"),
        ("CanBaoVe",  3.5f, "Bảo vệ",    "…Ừ. Nó xuống đây rồi."),
        ("TrenBan",   3.0f, "Tôi",       "Bộ tóc giả này… sao lại ở đây?"),
        ("QuayLai",   3.0f, "Giang hồ",  "Chạy đâu cho thoát hả em?"),
        ("DenMan",    2.0f, "",          ""),
    };

    [MenuItem("Tools/Dựng khung Cutscene BadEnd1 (bảo vệ là đồng bọn)")]
    static void Dung()
    {
        CutscenePlayer player = Build("BadEnd1", "BE1", Shots, new[] { "DienVien_Toi", "DienVien_CoGai", "BaoVe", "GiangHo" });
        if (player == null) return;
        var end = Object.FindFirstObjectByType<EndScreenManager>(FindObjectsInactive.Include);
        if (end != null)
            UnityEventTools.AddStringPersistentListener(OnFinished(player), end.ShowBadEnd,
                "Người duy nhất bạn tin tưởng… cũng là một trong số chúng. Không ai biết chuyện gì đã xảy ra ở phòng 302.");
    }

    /// Dựng khung Cutscene_{tag}: director + Timeline + camera + phụ đề. Trả về null nếu đã có.
    /// Player thật được thêm vào Hide During; On Finished để trống cho bên gọi tự gắn.
    public static CutscenePlayer Build(string tag, string camPrefix,
        (string shot, float dur, string who, string line)[] shots, string[] actors)
    {
        string rootName = "Cutscene_" + tag;
        if (GameObject.Find(rootName) != null)
        {
            Debug.LogError($"[Cutscene] Đã có {rootName} trong scene — xóa đi nếu muốn dựng lại.");
            return null;
        }
        SceneView sv = SceneView.lastActiveSceneView;
        Vector3 pos = sv != null ? sv.camera.transform.position : Vector3.up * 1.6f;
        Quaternion rot = sv != null ? sv.camera.transform.rotation : Quaternion.identity;

        // --- gốc + director ---
        var root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Dựng " + rootName);
        var director = root.AddComponent<PlayableDirector>();
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, AssetDatabase.GenerateUniqueAssetPath($"Assets/{rootName}Timeline.playable"));
        director.playableAsset = timeline;

        // --- camera thật của cutscene (Brain + AudioListener vì Player bị ẩn lúc chiếu) ---
        var camGo = new GameObject($"Cam_{tag}_Main");
        camGo.transform.SetParent(root.transform);
        camGo.transform.SetPositionAndRotation(pos, rot);
        var cam = camGo.AddComponent<Camera>();
        cam.depth = 20;
        camGo.AddComponent<AudioListener>();
        var brain = camGo.AddComponent<CinemachineBrain>();

        // --- camera ảo, mỗi shot 1 cái ---
        var camsRoot = new GameObject($"Cams_{tag}");
        camsRoot.transform.SetParent(root.transform);
        var vcams = new CinemachineCamera[shots.Length];
        for (int i = 0; i < shots.Length; i++)
        {
            var v = new GameObject($"Cam_{camPrefix}_{i + 1}_{shots[i].shot}");
            v.transform.SetParent(camsRoot.transform);
            v.transform.SetPositionAndRotation(pos + rot * new Vector3(0.3f * i, 0, 0), rot); // xếp lệch nhau cho dễ chọn
            vcams[i] = v.AddComponent<CinemachineCamera>();
        }

        // --- UI: viền đen, phụ đề, màn đen ---
        TMP_FontAsset font = null;
        foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.font != null) { font = t.font; break; } // dùng font đang hiển thị được tiếng Việt trong scene
        var canvasGo = new GameObject($"UI_{tag}", typeof(RectTransform));
        canvasGo.transform.SetParent(root.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject vien = MakeRect("VienDen", canvasGo.transform, Vector2.zero, Vector2.one, null);
        MakeRect("Tren", vien.transform, new Vector2(0, 0.88f), Vector2.one, Color.black);
        MakeRect("Duoi", vien.transform, Vector2.zero, new Vector2(1, 0.12f), Color.black);
        vien.SetActive(false);
        GameObject manDen = MakeRect("ManDen", canvasGo.transform, Vector2.zero, Vector2.one, Color.black);
        manDen.SetActive(false); // tạo trước phụ đề → phụ đề vẫn hiện trên màn đen (vd: "Sáng hôm sau…")

        // --- track ---
        var cmTrack = timeline.CreateTrack<CinemachineTrack>(null, "Camera (shot)");
        director.SetGenericBinding(cmTrack, brain);
        var vienTrack = timeline.CreateTrack<ActivationTrack>(null, "Vien den");
        director.SetGenericBinding(vienTrack, vien);
        var denTrack = timeline.CreateTrack<ActivationTrack>(null, "Man den");
        director.SetGenericBinding(denTrack, manDen);
        var subTrackParent = timeline.CreateTrack<GroupTrack>(null, "Phu de");
        foreach (string actor in actors)
            timeline.CreateTrack<AnimationTrack>(null, actor); // kéo diễn viên vào ô binding
        timeline.CreateTrack<AudioTrack>(null, "Am thanh");

        double t0 = 0;
        for (int i = 0; i < shots.Length; i++)
        {
            var shotClip = cmTrack.CreateClip<CinemachineShot>();
            shotClip.start = t0; shotClip.duration = shots[i].dur;
            shotClip.displayName = shots[i].shot;
            var shot = (CinemachineShot)shotClip.asset;
            shot.VirtualCamera.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(shot.VirtualCamera.exposedName, vcams[i]);

            if (shots[i].shot.StartsWith("DenMan"))
            {
                var dc = denTrack.CreateDefaultClip(); dc.start = t0; dc.duration = shots[i].dur;
            }
            if (!string.IsNullOrEmpty(shots[i].line))
            {
                string text = string.IsNullOrEmpty(shots[i].who) ? shots[i].line : $"<b>{shots[i].who}:</b> {shots[i].line}";
                GameObject sub = MakeSubtitle($"PhuDe_{i + 1}", canvasGo.transform, text, font);
                var act = timeline.CreateTrack<ActivationTrack>(subTrackParent, $"PhuDe_{i + 1}");
                director.SetGenericBinding(act, sub);
                var c = act.CreateDefaultClip(); c.start = t0 + 0.2; c.duration = shots[i].dur - 0.3;
            }
            t0 += shots[i].dur;
        }
        var vc = vienTrack.CreateDefaultClip(); vc.start = 0; vc.duration = t0;

        // --- CutscenePlayer: hiện camera + UI lúc chiếu ---
        var player = root.AddComponent<CutscenePlayer>();
        var so = new SerializedObject(player);
        var show = so.FindProperty("showDuring");
        show.arraySize = 3;
        show.GetArrayElementAtIndex(0).objectReferenceValue = camGo;
        show.GetArrayElementAtIndex(1).objectReferenceValue = canvasGo;
        show.GetArrayElementAtIndex(2).objectReferenceValue = camsRoot; // camera ảo tắt ngoài cutscene, kẻo giành camera chính
        GameObject realPlayer = GameObject.FindWithTag("Player");
        if (realPlayer != null)
        {
            var hide = so.FindProperty("hideDuring");
            hide.arraySize = 1;
            hide.GetArrayElementAtIndex(0).objectReferenceValue = realPlayer;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        Debug.Log($"[Cutscene] Đã dựng {rootName} ({t0:0.#}s, {shots.Length} shot). Mở Timeline để đặt góc từng camera và gắn diễn viên.", root);
        return player;
    }

    public static UnityEvent OnFinished(CutscenePlayer player)
    {
        var f = typeof(CutscenePlayer).GetField("onFinished", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f.GetValue(player) is not UnityEvent e) f.SetValue(player, e = new UnityEvent()); // vừa AddComponent: có thể còn null
        return e;
    }

    static GameObject MakeRect(string name, Transform parent, Vector2 min, Vector2 max, Color? color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        if (color.HasValue) { var img = go.AddComponent<Image>(); img.color = color.Value; img.raycastTarget = false; }
        return go;
    }

    static GameObject MakeSubtitle(string name, Transform parent, string text, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.1f, 0.02f); rt.anchorMax = new Vector2(0.9f, 0.11f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text; tmp.fontSize = 36; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        tmp.raycastTarget = false;
        go.SetActive(false);
        return go;
    }
}
