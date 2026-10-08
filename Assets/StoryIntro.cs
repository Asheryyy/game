using UnityEngine;
using System.Collections;

/// <summary>
/// Mở đầu game: vào scene → hiện dòng giới thiệu → nhân vật tự nói vài câu.
/// Câu cuối có thể giao quest đầu tiên (Quest To Give trong DialogueLine).
/// Gắn vào 1 GameObject trống trong SampleScene.
/// </summary>
public class StoryIntro : MonoBehaviour
{
    [Tooltip("Dòng chữ hiện lúc mở đầu (để trống = bỏ qua)")]
    [SerializeField] private string introCaption = "Tòa B - Chung cư cũ. Đêm đầu tiên ở nhà mới.";
    [Tooltip("Chờ bao lâu sau khi vào scene mới bắt đầu nói (giây)")]
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private DialogueLine[] introLines;

    IEnumerator Start()
    {
        if (!string.IsNullOrEmpty(introCaption) && QuestUI.Instance != null)
            QuestUI.Instance.ShowNotification(introCaption);

        yield return new WaitForSeconds(startDelay);

        if (introLines != null && introLines.Length > 0)
            DialogueManager.Instance.StartDialogue(introLines);
    }
}
