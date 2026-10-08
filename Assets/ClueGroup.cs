using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Nhóm manh mối: tìm đủ hết các ClueItem trong nhóm → hoàn thành quest,
/// nhân vật nói, bật object (vd: cửa phòng trong), gọi event.
/// Gắn vào 1 GameObject trống; kéo nó vào ô Group của từng ClueItem.
/// </summary>
public class ClueGroup : MonoBehaviour
{
    [SerializeField] private string questToComplete = "luc_soat_302";
    [Tooltip("Hiện tiến độ: 'Bằng chứng 1/3'")]
    [SerializeField] private string progressLabel = "Bằng chứng";
    [Tooltip("Nhân vật nói khi tìm đủ (dòng cuối có thể giao quest mới)")]
    [SerializeField] private DialogueLine[] doneLines;
    [SerializeField] private GameObject[] activateWhenDone;
    [SerializeField] private UnityEvent onAllFound;

    readonly List<ClueItem> clues = new List<ClueItem>();
    int found;
    bool done;

    public void Register(ClueItem c) { if (!clues.Contains(c)) clues.Add(c); }

    public void OnClueFound(ClueItem c)
    {
        if (done) return;
        found++;
        if (found < clues.Count)
        {
            if (QuestUI.Instance != null)
                QuestUI.Instance.ShowNotification($"{progressLabel} {found}/{clues.Count}");
            return;
        }

        done = true;
        if (!string.IsNullOrEmpty(questToComplete) && QuestManager.Instance != null)
            QuestManager.Instance.CompleteQuest(questToComplete);
        foreach (GameObject go in activateWhenDone) if (go != null) go.SetActive(true);
        StartCoroutine(PlayDoneLines());
        onAllFound?.Invoke();
    }

    // chờ thoại của manh mối cuối xong rồi mới nói câu tổng kết
    System.Collections.IEnumerator PlayDoneLines()
    {
        if (doneLines == null || doneLines.Length == 0 || DialogueManager.Instance == null) yield break;
        yield return null;
        while (DialogueManager.Instance.IsActive) yield return null;
        DialogueManager.Instance.StartDialogue(doneLines);
    }
}
