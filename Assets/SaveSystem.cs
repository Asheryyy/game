using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// Script nào có trạng thái riêng cần lưu (đã ngủ, đã mở cửa...) thì implement interface này.
public interface ISaveable
{
    string CaptureState();
    void RestoreState(string state);
}

[Serializable]
public class SaveData
{
    public string scene;
    public string savedAt;
    public Vector3 playerPos;
    public float playerYaw;
    public int timeOfDay = -1;
    public List<Quest> quests = new List<Quest>();
    public List<string> clues = new List<string>();
    public List<string> activeOn = new List<string>();   // object bật khác lúc đầu scene
    public List<string> activeOff = new List<string>();  // object tắt khác lúc đầu scene
    public List<string> stateKeys = new List<string>();
    public List<string> stateValues = new List<string>();
}

/// <summary>
/// Lưu / tải game ra file JSON (Application.persistentDataPath/save.json).
/// Tự tạo khi chạy game, không cần gắn vào scene.
/// - Save(): lưu nhiệm vụ, manh mối, vị trí player, giờ trong ngày, object bật/tắt, trạng thái các script.
/// - Tự lưu mỗi khi nhận nhiệm vụ mới (checkpoint).
/// - Menu "Tiếp tục": LoadOnNextScene = true rồi load scene → tự khôi phục.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    static SaveSystem inst;

    /// Đặt TRUE trước khi load scene từ menu "Tiếp tục"
    public static bool LoadOnNextScene;
    /// Đang khôi phục save (StoryIntro... dùng để bỏ qua đoạn mở đầu)
    public static bool IsLoading { get; private set; }

    static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
    public static bool HasSave => File.Exists(FilePath);

    public static SaveData ReadSave()
    {
        try { return HasSave ? JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) : null; }
        catch (Exception e) { Debug.LogWarning("[Save] Không đọc được file save: " + e.Message); return null; }
    }

    public static void DeleteSave()
    {
        if (HasSave) File.Delete(FilePath);
    }

    // trạng thái bật/tắt lúc scene vừa load → chỉ lưu những gì đã thay đổi
    readonly Dictionary<string, bool> defaults = new Dictionary<string, bool>();
    Coroutine autoSave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        GameObject go = new GameObject("[SaveSystem]");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<SaveSystem>();
        SceneManager.sceneLoaded += inst.OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        defaults.Clear();
        foreach (Transform t in AllTransforms(scene)) defaults[PathOf(t)] = t.gameObject.activeSelf;

        IsLoading = false;
        if (!LoadOnNextScene) return;
        LoadOnNextScene = false;

        SaveData d = ReadSave();
        if (d == null || d.scene != scene.name) return;
        IsLoading = true;
        StartCoroutine(LoadRoutine(scene, d));
    }

    // ──────────────────────────────────────────────
    //  LƯU
    // ──────────────────────────────────────────────

    /// Có lưu được lúc này không (không đang thoại / đọc giấy / bị đuổi / bị đánh ngất)
    public static bool CanSave(out string reason)
    {
        reason = null;
        if (QuestManager.Instance == null) { reason = "Không có gì để lưu ở đây"; return false; }
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) { reason = "Đang hội thoại"; return false; }
        if (DocumentViewer.Instance != null && DocumentViewer.Instance.IsOpen) { reason = "Đang đọc giấy tờ"; return false; }
        if (KnockoutEvent.AnyRunning) { reason = "Không thể lưu lúc này"; return false; }
        foreach (ChaserEnemy c in FindObjectsByType<ChaserEnemy>(FindObjectsSortMode.None))
            if (c.isActiveAndEnabled) { reason = "Không thể lưu khi đang bị truy đuổi!"; return false; }
        return true;
    }

    /// Lưu ngay. Trả về false nếu không lưu được (lý do ở reason)
    public static bool Save(out string reason)
    {
        if (inst == null) { reason = "Lỗi hệ thống lưu"; return false; }
        if (!CanSave(out reason)) return false;
        inst.WriteSave();
        return true;
    }

    /// Tự lưu (checkpoint): chờ tới lúc lưu được rồi mới lưu, không báo gì
    public static void RequestAutoSave()
    {
        if (inst == null || IsLoading) return;
        if (inst.autoSave != null) inst.StopCoroutine(inst.autoSave);
        inst.autoSave = inst.StartCoroutine(inst.AutoSaveRoutine());
    }

    IEnumerator AutoSaveRoutine()
    {
        yield return null;
        float waited = 0f;
        string reason;
        while (!CanSave(out reason) && waited < 30f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (CanSave(out reason)) WriteSave();
        autoSave = null;
    }

    void WriteSave()
    {
        Scene scene = SceneManager.GetActiveScene();
        SaveData d = new SaveData { scene = scene.name, savedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm") };

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) { d.playerPos = player.transform.position; d.playerYaw = player.transform.eulerAngles.y; }
        if (TimeOfDaySwitch.Instance != null) d.timeOfDay = (int)TimeOfDaySwitch.Instance.Current;
        if (QuestManager.Instance != null) d.quests = new List<Quest>(QuestManager.Instance.GetActiveQuests());
        d.clues = new List<string>(ClueItem.AllFound);

        foreach (Transform t in AllTransforms(scene))
        {
            string p = PathOf(t);
            bool a = t.gameObject.activeSelf;
            if (defaults.TryGetValue(p, out bool def) && def == a) continue;
            (a ? d.activeOn : d.activeOff).Add(p);
        }

        foreach (var (key, s) in AllSaveables(scene))
        {
            d.stateKeys.Add(key);
            d.stateValues.Add(s.CaptureState());
        }

        File.WriteAllText(FilePath, JsonUtility.ToJson(d));
        Debug.Log($"[Save] Đã lưu ({d.quests.Count} nhiệm vụ, {d.clues.Count} manh mối) → {FilePath}");
    }

    // ──────────────────────────────────────────────
    //  TẢI
    // ──────────────────────────────────────────────

    IEnumerator LoadRoutine(Scene scene, SaveData d)
    {
        yield return null; // chờ các Start() chạy xong

        // 1. bật/tắt object
        var map = new Dictionary<string, GameObject>();
        foreach (Transform t in AllTransforms(scene)) map[PathOf(t)] = t.gameObject;
        foreach (string p in d.activeOff) if (map.TryGetValue(p, out GameObject go)) go.SetActive(false);
        foreach (string p in d.activeOn)  if (map.TryGetValue(p, out GameObject go)) go.SetActive(true);

        // 2. nhiệm vụ + manh mối
        if (QuestManager.Instance != null) QuestManager.Instance.RestoreQuests(d.quests);
        ClueItem.RestoreFound(d.clues);

        // 3. trạng thái từng script
        var states = new Dictionary<string, string>();
        for (int i = 0; i < d.stateKeys.Count && i < d.stateValues.Count; i++) states[d.stateKeys[i]] = d.stateValues[i];
        foreach (var (key, s) in AllSaveables(scene))
            if (states.TryGetValue(key, out string v))
            {
                try { s.RestoreState(v); }
                catch (Exception e) { Debug.LogWarning($"[Save] Lỗi khôi phục {key}: {e.Message}"); }
            }

        // 4. giờ trong ngày
        if (TimeOfDaySwitch.Instance != null && d.timeOfDay >= 0) TimeOfDaySwitch.Instance.SetMode(d.timeOfDay);

        // 5. vị trí player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(d.playerPos, Quaternion.Euler(0f, d.playerYaw, 0f));
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            if (cc != null) cc.enabled = true;
        }

        if (QuestUI.Instance != null)
        {
            QuestUI.Instance.UpdateUI();
            QuestUI.Instance.ShowNotification("Đã tải game (" + d.savedAt + ")");
        }
        IsLoading = false;
        Debug.Log("[Save] Đã tải save " + d.savedAt);
    }

    // ──────────────────────────────────────────────
    //  Tiện ích
    // ──────────────────────────────────────────────

    // Bỏ qua UI (RectTransform): panel thông báo, tờ giấy... không cần lưu
    static IEnumerable<Transform> AllTransforms(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (!(t is RectTransform)) yield return t;
    }

    static IEnumerable<(string, ISaveable)> AllSaveables(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(mb is ISaveable s)) continue;
                // nhiều component cùng loại trên 1 object → thêm số thứ tự
                int idx = Array.IndexOf(mb.GetComponents(mb.GetType()), mb);
                yield return (PathOf(mb.transform) + "|" + mb.GetType().Name + "|" + idx, s);
            }
    }

    // đường dẫn duy nhất: tên + vị trí trong cha (2 object trùng tên vẫn phân biệt được)
    static string PathOf(Transform t)
    {
        var sb = new StringBuilder();
        for (; t != null; t = t.parent) sb.Insert(0, "/" + t.name + "#" + t.GetSiblingIndex());
        return sb.ToString();
    }

    static readonly System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
    public static string Rot(Quaternion q) => string.Join(",", q.x.ToString(IC), q.y.ToString(IC), q.z.ToString(IC), q.w.ToString(IC));
    public static Quaternion ParseRot(string s)
    {
        string[] p = s.Split(',');
        return new Quaternion(float.Parse(p[0], IC), float.Parse(p[1], IC), float.Parse(p[2], IC), float.Parse(p[3], IC));
    }
}
