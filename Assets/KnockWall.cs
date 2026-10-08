using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Bức tường có tiếng gõ SOS (... --- ...) vọng sang từ phòng bên cạnh.
/// Bật object này lên → tiếng gõ lặp lại mỗi vài giây.
/// Player lại gần + bấm E → gõ đáp lại → bên kia gõ trả lời → nhân vật nói, xong quest.
/// Gắn vào object có Collider (Is Trigger) sát bức tường.
/// </summary>
public class KnockWall : MonoBehaviour
{
    [Header("=== AM THANH ===")]
    [Tooltip("Tiếng gõ từ BÊN KIA tường: AudioSource 3D (Spatial Blend = 1) đặt sau tường, Output = SFX")]
    [SerializeField] private AudioSource otherSideKnock;
    [Tooltip("Tiếng nhân vật tự gõ: AudioSource 2D, Output = SFX")]
    [SerializeField] private AudioSource playerKnock;
    [Tooltip("1 tiếng 'cộc' ngắn")]
    [SerializeField] private AudioClip knockClip;
    [Tooltip("Thêm các tiếng 'cộc' khác → mỗi lần gõ chọn ngẫu nhiên cho tự nhiên (không bắt buộc)")]
    [SerializeField] private AudioClip[] extraKnockClips;

    [Header("=== NHIP GO ===")]
    [SerializeField] private float shortGap = 0.25f; // giữa 2 tiếng gõ nhanh
    [SerializeField] private float longGap = 0.7f;   // giữa 2 tiếng gõ chậm
    [Tooltip("Lặp lại SOS mỗi bao nhiêu giây khi chưa gõ đáp")]
    [SerializeField] private float repeatEvery = 6f;

    [Header("=== TUONG TAC ===")]
    [SerializeField] private string hintText = "Bấm [E] để gõ lại vào tường";
    [Tooltip("Hoàn thành quest này khi gõ đáp (vd: tieng_go)")]
    [SerializeField] private string questToComplete = "tieng_go";
    [Tooltip("Nhân vật nói sau khi bên kia gõ trả lời (dòng cuối có thể giao quest mới)")]
    [SerializeField] private DialogueLine[] afterReplyLines;
    [SerializeField] private UnityEvent onAnswered;

    bool playerNearby, answered, busy;

    void OnEnable() => StartCoroutine(SosLoop());

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    IEnumerator SosLoop()
    {
        yield return new WaitForSeconds(1.5f);
        while (!answered)
        {
            if (!busy) yield return KnockPattern(otherSideKnock, true);
            yield return new WaitForSeconds(repeatEvery);
        }
    }

    // SOS = 3 nhanh, 3 chậm, 3 nhanh
    IEnumerator KnockPattern(AudioSource src, bool sos)
    {
        if (src == null || (knockClip == null && (extraKnockClips == null || extraKnockClips.Length == 0))) yield break;
        float[] gaps = sos
            ? new[] { shortGap, shortGap, longGap, longGap, longGap, longGap, shortGap, shortGap, 0f }
            : new[] { shortGap, shortGap, 0f }; // nhân vật gõ 3 tiếng
        foreach (float g in gaps)
        {
            src.pitch = Random.Range(0.95f, 1.05f); // cho tiếng gõ đỡ giống hệt nhau
            src.PlayOneShot(RandomClip());
            yield return new WaitForSeconds(Mathf.Max(g, 0.05f));
        }
    }

    AudioClip RandomClip()
    {
        int extra = extraKnockClips != null ? extraKnockClips.Length : 0;
        int i = Random.Range(knockClip != null ? -1 : 0, extra); // -1 = knockClip
        AudioClip c = i < 0 ? knockClip : extraKnockClips[i];
        return c != null ? c : knockClip;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || answered) return;
        playerNearby = true;
        if (QuestUI.Instance != null) QuestUI.Instance.ShowNotification(hintText);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerNearby = false;
    }

    void Update()
    {
        if (!playerNearby || answered || busy) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) return;
        if (Keyboard.current.eKey.wasPressedThisFrame) StartCoroutine(Answer());
    }

    IEnumerator Answer()
    {
        busy = true;
        answered = true;

        yield return KnockPattern(playerKnock, false);      // mình gõ 3 cái
        yield return new WaitForSeconds(1.5f);               // im lặng... hồi hộp
        yield return KnockPattern(otherSideKnock, true);     // bên kia gõ trả lời gấp
        yield return new WaitForSeconds(0.8f);

        if (!string.IsNullOrEmpty(questToComplete) && QuestManager.Instance != null)
            QuestManager.Instance.CompleteQuest(questToComplete);
        if (afterReplyLines != null && afterReplyLines.Length > 0 && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(afterReplyLines);
        onAnswered?.Invoke();
        busy = false;
    }
}
