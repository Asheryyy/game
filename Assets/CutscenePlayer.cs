using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

/// <summary>
/// Phát cutscene Timeline (vd: Cutscene_BadEnd2 — đánh nhau với giang hồ rồi bị đánh gục).
/// Gắn vào object có PlayableDirector (Play On Awake = tắt, Wrap Mode = None).
/// Gọi Play() từ UnityEvent (vd: ChaserEnemy On Caught) thay cho EndScreenManager.ShowBadEnd,
/// rồi kéo ShowBadEnd vào On Finished để hiện màn kết thúc sau khi chiếu xong.
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class CutscenePlayer : MonoBehaviour
{
    [Tooltip("Chỉ bật lúc chiếu: CutScene_Cam, Player_CutScene, giang hồ của cutscene...")]
    [SerializeField] private GameObject[] showDuring;
    [Tooltip("Tắt lúc chiếu: Player, giang hồ đang đuổi (ChaserEnemy)...")]
    [SerializeField] private GameObject[] hideDuring;
    [Tooltip("Gọi khi chiếu xong (vd: EndScreenManager.ShowBadEnd)")]
    [SerializeField] private UnityEvent onFinished;
    [Tooltip("Thoại sau khi chiếu xong — câu nào có Quest To Give sẽ giao quest (vd: đi ngủ qua ngày 4)")]
    [SerializeField] private DialogueLine[] linesAfter;

    PlayableDirector director;
    bool played;

    void Awake()
    {
        director = GetComponent<PlayableDirector>();
        director.stopped += _ =>
        {
            onFinished?.Invoke();
            if (linesAfter != null && linesAfter.Length > 0 && DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(linesAfter);
        };
        // diễn viên + camera cutscene nằm sẵn trong scene → giấu đi tới lúc chiếu
        foreach (GameObject go in showDuring) if (go != null) go.SetActive(false);
    }

    [ContextMenu("Play (test)")] // lúc đang Play game: chuột phải component → Play (test) để xem cutscene ngay
    public void Play()
    {
        if (played) return;
        played = true;
        foreach (GameObject go in hideDuring) if (go != null) go.SetActive(false);
        foreach (GameObject go in showDuring) if (go != null) go.SetActive(true);
        director.Play();
    }
}
