using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Reflection;

/// <summary>
/// Tools → Dựng Good End "Im lặng là vàng" (chạy 1 lần trong SampleScene):
///   1. GiayNhan_1: tờ giấy "Im lặng là vàng" dưới chân chỗ tỉnh dậy sau khi bị đánh (SuKien_BiDanh bật lên).
///   2. GiayNhan_2: tờ giấy dán ở cửa 303, hiện khi Cutscene_ChayVeNha chiếu xong → chọn Tin / Không tin.
///      Tin → manh mối "tin_tuong".
///   3. GoodEnd_GoiDien: CuaSo_Thoat giờ gọi ClueBranch.Run thay cho Cutscene_BadEnd1.Play:
///      có tin_tuong → gọi điện → Good End; không → Cutscene_BadEnd1 như cũ.
/// Sau khi chạy: chỉnh vị trí 2 tờ giấy cho khớp sàn/cửa rồi Ctrl+S. Text sửa trong Inspector.
/// </summary>
public static class DungGoodEndTinTuong
{
    const string Clue = "tin_tuong";

    [MenuItem("Tools/Dựng Good End \"Im lặng là vàng\" (tin tưởng người lạ)")]
    static void Build()
    {
        if (Find("GoodEnd_GoiDien") != null) { Debug.LogError("[GoodEnd] Đã dựng rồi (có GoodEnd_GoiDien). Xóa GiayNhan_1, GiayNhan_2, GoodEnd_GoiDien để dựng lại."); return; }

        KnockoutEvent knock = Find("SuKien_BiDanh")?.GetComponent<KnockoutEvent>();
        CutscenePlayer chayVeNha = Find("Cutscene_ChayVeNha")?.GetComponent<CutscenePlayer>();
        CutscenePlayer badEnd1 = Find("Cutscene_BadEnd1")?.GetComponent<CutscenePlayer>();
        TeleportZone cuaSo = Find("CuaSo_Thoat")?.GetComponent<TeleportZone>();
        Transform cua303 = Find("Cua303_Vao");
        EndScreenManager end = Object.FindFirstObjectByType<EndScreenManager>(FindObjectsInactive.Include);
        if (knock == null || chayVeNha == null || badEnd1 == null || cuaSo == null || cua303 == null || end == null)
        {
            Debug.LogError($"[GoodEnd] Thiếu object: SuKien_BiDanh={knock != null} Cutscene_ChayVeNha={chayVeNha != null} " +
                           $"Cutscene_BadEnd1={badEnd1 != null} CuaSo_Thoat={cuaSo != null} Cua303_Vao={cua303 != null} EndScreenManager={end != null}");
            return;
        }

        // lần trước dựng dở (lỗi giữa chừng) → dọn tờ giấy cũ + listener trỏ tới nó rồi dựng lại
        var keep = new System.Collections.Generic.Dictionary<string, (Vector3 p, Quaternion r, Vector3 s)>();
        foreach (string old in new[] { "GiayNhan_1", "GiayNhan_2" })
        {
            Transform t = Find(old);
            if (t == null) continue;
            RemoveListenersTo(knock, "onBlackout", t.gameObject);
            RemoveListenersTo(chayVeNha, "onFinished", t.gameObject);
            keep[old] = (t.position, t.rotation, t.localScale); // giữ chỗ bạn đã kéo
            Undo.DestroyObjectImmediate(t.gameObject);
        }

        // ── 1. Tờ giấy 1: dưới chân chỗ tỉnh dậy ──
        Transform wake = Get(knock, "wakeUpPoint") as Transform;
        if (wake == null) wake = Find("Guong") ?? knock.transform; // Unity null: không dùng ?? với field serialize
        ClueItem note1 = MakePaper("GiayNhan_1", Floor(wake.position + wake.forward * 0.7f), Quaternion.Euler(90f, wake.eulerAngles.y, 0f));
        Restore(note1, keep);
        Set(note1, "clueId", "giay_im_lang");
        Set(note1, "hintText", "Bấm [E] để nhặt mảnh giấy");
        Set(note1, "documentText", "Im lặng là vàng.");
        Set(note1, "beforeReadLines", Lines("Có mảnh giấy... ngay dưới chân mình?"));
        Set(note1, "thoughts", Lines(
            "\"Im lặng là vàng\"...",
            "Kẻ đánh mình để lại cái này. Hắn muốn mình ngậm miệng lại.",
            "Nhưng nếu muốn mình im lặng mãi mãi thì hắn đã ra tay luôn rồi... Hắn đang cảnh cáo mình sao?"));
        Set(note1, "hideAfterFound", true);
        AddListener(knock, "onBlackout", note1.gameObject);

        // ── 2. Tờ giấy 2: dán trên cửa 303, sau khi chạy thoát về chung cư ──
        ClueItem note2 = MakePaper("GiayNhan_2", cua303.position + Vector3.up * 1.4f, cua303.rotation);
        Restore(note2, keep);
        Set(note2, "clueId", "giay_so_dien_thoai");
        Set(note2, "autoTrigger", true);
        Set(note2, "documentText",
            "Ta đã bảo rồi, cứ im lặng mà làm việc là tốt nhất.\n\n" +
            "Đây là số liên hệ của ta, nếu ngươi tin tưởng thì lưu vào, có ngày sẽ sử dụng.\n\n" +
            "0909.141.302");
        Set(note2, "beforeReadLines", Lines("Lại một tờ giấy... dán ngay trên cửa phòng mình."));
        Set(note2, "thoughts", Lines(
            "Lại là hắn. Hắn biết cả chuyện mình vừa thoát khỏi bọn giang hồ.",
            "Hắn là ai? Đồng bọn của chúng... hay là người đang âm thầm giúp mình?"));

        ChoiceEvent luuSo = note2.gameObject.AddComponent<ChoiceEvent>();
        Set(luuSo, "prompt", "Có nên lưu số điện thoại lạ này không?");
        var tin = new ChoiceOption
        {
            label = "Tin tưởng — lưu số lại",
            lines = Lines("Dù sao hắn cũng đã tha cho mình một lần. Cứ lưu lại, biết đâu có ngày cần đến."),
            onChosen = new UnityEvent()
        };
        UnityEventTools.AddStringPersistentListener(tin.onChosen, note2.GiveClue, Clue);
        var khongTin = new ChoiceOption
        {
            label = "Không tin — vò nát tờ giấy",
            lines = Lines("Kẻ đã đánh ngất mình thì tin sao được. Vứt đi cho rồi."),
            onChosen = new UnityEvent()
        };
        Set(luuSo, "options", new[] { tin, khongTin });
        UnityEventTools.AddVoidPersistentListener(GetEvent(note2, "onFound"), luuSo.Play);
        AddListener(chayVeNha, "onFinished", note2.gameObject);

        // ── 3. Rẽ nhánh ở cửa sổ thoát thân ──
        var root = new GameObject("GoodEnd_GoiDien");
        Undo.RegisterCreatedObjectUndo(root, "Dựng Good End");
        root.transform.position = cuaSo.transform.position;

        ChoiceEvent goiDien = root.AddComponent<ChoiceEvent>();
        Set(goiDien, "prompt", "Bọn chúng đang lùng sục khắp tầng 3...\nSố điện thoại trên tờ giấy hôm qua!");
        var goi = new ChoiceOption
        {
            label = "Gọi vào số điện thoại đã lưu",
            lines = new[]
            {
                new DialogueLine { speakerName = "Tôi", text = "Alo... là anh phải không? Tôi đang ở phòng 303, cô gái bị bắt cóc ở 302 đang ở với tôi!" },
                new DialogueLine { speakerName = "???", text = "Ta biết. Khóa cửa lại, đừng mở cho bất kỳ ai — kể cả tên bảo vệ." },
                new DialogueLine { speakerName = "???", text = "Năm phút nữa người của ta tới. Lần này thì đừng im lặng nữa." },
            },
            onChosen = new UnityEvent()
        };
        UnityEventTools.AddStringPersistentListener(goi.onChosen, end.ShowGoodEnd,
            "Tiếng còi cảnh sát vang khắp khu chung cư.\n\n" +
            "Người để lại những mảnh giấy là một cảnh sát chìm đã theo dõi đường dây suốt nhiều tháng — kể cả tên bảo vệ.\n\n" +
            "Cô gái được giải cứu an toàn. Đôi khi, biết im lặng đúng lúc mới là cách để sống sót.");
        Set(goiDien, "options", new[] { goi });

        ClueBranch branch = root.AddComponent<ClueBranch>();
        Set(branch, "clueId", Clue);
        UnityEventTools.AddVoidPersistentListener(GetEvent(branch, "ifFound"), goiDien.Play);
        UnityEventTools.AddVoidPersistentListener(GetEvent(branch, "ifNotFound"), badEnd1.Play);

        // CuaSo_Thoat: bỏ Cutscene_BadEnd1.Play, thay bằng ClueBranch.Run
        Undo.RecordObject(cuaSo, "Dựng Good End");
        UnityEventBase onTp = GetEvent(cuaSo, "onTeleported");
        for (int i = onTp.GetPersistentEventCount() - 1; i >= 0; i--)
            if (onTp.GetPersistentTarget(i) == badEnd1 && onTp.GetPersistentMethodName(i) == "Play")
                UnityEventTools.RemovePersistentListener(onTp, i);
        UnityEventTools.AddVoidPersistentListener(onTp, branch.Run);
        EditorUtility.SetDirty(cuaSo);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.objects = new Object[] { note1.gameObject, note2.gameObject, root };
        Debug.Log("[GoodEnd] Đã dựng xong. Chỉnh vị trí GiayNhan_1 (dưới chân chỗ tỉnh dậy) và GiayNhan_2 (trên cửa 303) rồi Ctrl+S.");
    }

    // (tên shot, giây, người nói, phụ đề) — shot "DenMan..." = màn đen
    static readonly (string shot, float dur, string who, string line)[] GoodEndShots =
    {
        ("RaHanhLang",   3.0f, "",          ""),
        ("CanMieng_1",   3.5f, "Tôi",       "Alo… tôi gọi theo số trên tờ giấy. Cô gái ở 302… tôi cứu ra được rồi!"),
        ("CanMieng_2",   4.0f, "???",       "Ta biết. Chạy hết hành lang, xuống cầu thang. Người của ta đang đợi sẵn ở dưới."),
        ("CanMieng_3",   2.5f, "Tôi",       "Được… được rồi!"),
        ("ChayHanhLang", 3.5f, "",          ""),
        ("CauThang",     3.0f, "",          ""),
        ("XeCho",        3.5f, "Người lạ",  "Lên xe mau! Bám chắc vào!"),
        ("XeChayDi",     3.0f, "",          ""),
        ("DenMan_Sang",  2.5f, "",          "Sáng hôm sau…"),
        ("CongAnToi",    3.5f, "Công an",   "Tất cả đứng yên! Công an đây!"),
        ("TomGon",       4.0f, "",          ""),
        ("DenMan",       2.0f, "",          ""),
    };

    /// Tools → Dựng khung Cutscene GoodEnd: ra hành lang, cận miệng gọi điện, chạy xuống có người chở đi,
    /// sáng hôm sau công an tóm gọn băng nhóm. Nối vào nút "Gọi vào số điện thoại đã lưu" (GoodEnd_GoiDien).
    /// Chạy SAU menu "Dựng Good End Im lặng là vàng".
    [MenuItem("Tools/Dựng khung Cutscene GoodEnd (gọi điện, chạy thoát, công an tóm gọn)")]
    static void BuildCutscene()
    {
        if (Find("GoodEnd_GoiDien") == null) Build(); // chưa dựng tờ giấy + nhánh → dựng luôn
        ChoiceEvent goiDien = Find("GoodEnd_GoiDien")?.GetComponent<ChoiceEvent>();
        EndScreenManager end = Object.FindFirstObjectByType<EndScreenManager>(FindObjectsInactive.Include);
        if (goiDien == null || end == null) { Debug.LogError($"[GoodEnd] Dựng phần Good End bị lỗi — xem lỗi đỏ phía trên (GoodEnd_GoiDien={goiDien != null}, EndScreenManager={end != null})"); return; }

        CutscenePlayer cut = DungCutsceneBadEnd1.Build("GoodEnd", "GE", GoodEndShots,
            new[] { "DienVien_Toi", "DienVien_CoGai", "NguoiCho", "Xe", "CongAn", "GiangHo", "BaoVe" });
        if (cut == null) return;

        // ẩn giống BadEnd1 (player + giang hồ đang đuổi...)
        CutscenePlayer bad1 = Find("Cutscene_BadEnd1")?.GetComponent<CutscenePlayer>();
        if (bad1 != null) Set(cut, "hideDuring", ((GameObject[])Get(bad1, "hideDuring")).Clone());

        UnityEventTools.AddStringPersistentListener(DungCutsceneBadEnd1.OnFinished(cut), end.ShowGoodEnd,
            "Người để lại những mảnh giấy là một cảnh sát chìm đã theo dõi đường dây suốt nhiều tháng — kể cả tên bảo vệ.\n\n" +
            "Sáng hôm sau, công an ập vào tòa B và tóm gọn cả băng nhóm. Cô gái được trở về với gia đình.\n\n" +
            "Đôi khi, biết im lặng đúng lúc mới là cách để sống sót.");

        // nút gọi điện: thoại chuyển vào cutscene, bấm xong là chiếu
        Undo.RecordObject(goiDien, "Dựng Cutscene GoodEnd");
        var opts = (ChoiceOption[])Get(goiDien, "options");
        opts[0].lines = new DialogueLine[0];
        opts[0].onChosen = new UnityEvent();
        UnityEventTools.AddVoidPersistentListener(opts[0].onChosen, cut.Play);
        Set(goiDien, "options", opts);

        EditorSceneManager.MarkSceneDirty(cut.gameObject.scene);
        Debug.Log("[GoodEnd] Đã nối nút \"Gọi vào số điện thoại đã lưu\" → Cutscene_GoodEnd → GOOD END.", cut);
    }

    // ──────────────────────────────────────────────

    static ClueItem MakePaper(string name, Vector3 pos, Quaternion rot)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Undo.RegisterCreatedObjectUndo(go, "Dựng Good End");
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // ClueItem tự thêm trigger lúc chạy
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = new Vector3(0.2f, 0.28f, 1f);
        go.SetActive(false); // bật bằng sự kiện
        return go.AddComponent<ClueItem>();
    }

    static void Restore(ClueItem note, System.Collections.Generic.Dictionary<string, (Vector3 p, Quaternion r, Vector3 s)> keep)
    {
        if (!keep.TryGetValue(note.name, out var k)) return;
        note.transform.SetPositionAndRotation(k.p, k.r);
        note.transform.localScale = k.s;
    }

    static Vector3 Floor(Vector3 p) =>
        Physics.Raycast(p + Vector3.up, Vector3.down, out RaycastHit hit, 5f) ? hit.point + Vector3.up * 0.01f : p;

    static DialogueLine[] Lines(params string[] texts) =>
        texts.Select(t => new DialogueLine { speakerName = "Tôi", text = t }).ToArray();

    // thêm "SetActive(true) cho target" vào UnityEvent field của comp
    static void AddListener(Component comp, string field, GameObject target)
    {
        Undo.RecordObject(comp, "Dựng Good End");
        UnityEventTools.AddBoolPersistentListener(GetEvent(comp, field), target.SetActive, true);
        EditorUtility.SetDirty(comp);
    }

    // component vừa AddComponent trong Editor: field UnityEvent còn null → tạo mới
    static UnityEventBase GetEvent(Component c, string field)
    {
        if (Get(c, field) is UnityEventBase e) return e;
        e = (UnityEventBase)System.Activator.CreateInstance(Field(c, field).FieldType);
        Set(c, field, e);
        return e;
    }

    static void RemoveListenersTo(Component comp, string field, Object target)
    {
        UnityEventBase e = GetEvent(comp, field);
        Undo.RecordObject(comp, "Dựng Good End");
        for (int i = e.GetPersistentEventCount() - 1; i >= 0; i--)
            if (e.GetPersistentTarget(i) == target) UnityEventTools.RemovePersistentListener(e, i);
        EditorUtility.SetDirty(comp);
    }

    static object Get(Component c, string field) => Field(c, field).GetValue(c);

    static void Set(Component c, string field, object value)
    {
        Field(c, field).SetValue(c, value);
        EditorUtility.SetDirty(c);
    }

    static FieldInfo Field(Component c, string name) =>
        c.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new System.MissingFieldException(c.GetType().Name, name);

    // tìm cả object đang tắt (Guong, Cutscene... tắt lúc đầu)
    static Transform Find(string name) =>
        Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
              .FirstOrDefault(t => t.name == name && !(t is RectTransform));
}
