using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Nhóm các việc dọn dẹp. Đếm số việc đã xong, hiện "Đã dọn 2/5".
/// Xong hết → hoàn thành quest, nhân vật tự nói vài câu, bật object (vd: tờ giấy note).
/// Gắn vào 1 GameObject trống; kéo nó vào ô Group của từng ChoreItem.
/// </summary>
public class ChoreGroup : MonoBehaviour, ISaveable
{
    [Tooltip("Quest hoàn thành khi dọn xong hết (vd: don_dep)")]
    [SerializeField] private string questToComplete = "don_dep";
    [Tooltip("Nhân vật nói khi dọn xong hết (dòng cuối có thể giao quest mới)")]
    [SerializeField] private DialogueLine[] doneLines;
    [Tooltip("Bật các object này khi dọn xong (vd: tờ giấy note của người cũ)")]
    [SerializeField] private GameObject[] activateWhenDone;
    [SerializeField] private UnityEvent onAllDone;

    readonly List<ChoreItem> chores = new List<ChoreItem>();
    int doneCount;

    public string CaptureState() => doneCount.ToString();
    public void RestoreState(string s) => doneCount = int.Parse(s);

    public void Register(ChoreItem c) { if (!chores.Contains(c)) chores.Add(c); }

    public void OnChoreDone(ChoreItem c)
    {
        doneCount++;
        if (doneCount < chores.Count)
        {
            Show($"Đã dọn {doneCount}/{chores.Count}");
            return;
        }

        if (!string.IsNullOrEmpty(questToComplete) && QuestManager.Instance != null)
            QuestManager.Instance.CompleteQuest(questToComplete);
        else Show("Đã dọn xong!");

        foreach (GameObject go in activateWhenDone) if (go != null) go.SetActive(true);
        if (doneLines != null && doneLines.Length > 0 && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(doneLines);
        onAllDone?.Invoke();
    }

    static void Show(string msg)
    {
        if (QuestUI.Instance != null) QuestUI.Instance.ShowNotification(msg);
    }
}
