using UnityEngine;

public class Trigger : MonoBehaviour, ISaveable
{
    [SerializeField] private Quest questToGive;
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private string requiredQuestId;
    [SerializeField] private DialogueLine[] dialogueBefore;
    [SerializeField] private DialogueLine[] dialogueDuring;
    [SerializeField] private DialogueLine[] dialogueAfter;
    [Tooltip("TRUE = chỉ nói chuyện 1 lần, đi ngang lần sau không nói lại")]
    [SerializeField] private bool playOnce = false;
    private bool hasPlayed = false;
    public string CaptureState() => hasPlayed ? "1" : "0";
    public void RestoreState(string s) => hasPlayed = s == "1";
    [Tooltip("Hoàn thành quest này ngay khi bắt đầu nói chuyện (vd: quest 'Hỏi đường cô 3')")]
    [SerializeField] private string questToCompleteOnTalk;
    [Tooltip("Bật các object này sau khi nói chuyện (vd: cái gương phát sáng cuối hành lang)")]
    [SerializeField] private GameObject[] activateAfterTalk;
    void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player") || DialogueManager.Instance.IsActive)
            return;
        if(playOnce && hasPlayed) return;

        // Nếu không có QuestManager → chỉ dùng dialogueBefore đơn giản
        if(QuestManager.Instance == null)
        {
            if(dialogueBefore != null && dialogueBefore.Length > 0)
            {
                DialogueManager.Instance.StartDialogue(dialogueBefore);
                hasPlayed = true;
            }
            return;
        }

        if(!string.IsNullOrEmpty(requiredQuestId) && !QuestManager.Instance.IsCompleted(requiredQuestId))
        {
            Quest reqQ = QuestManager.Instance.GetQuest(requiredQuestId);
            string reqName = reqQ != null ? reqQ.questName : requiredQuestId;
            QuestUI.Instance.ShowNotification("Cần hoàn thành " + reqName + " trước");
            return;
        }

        Quest currentQ = QuestManager.Instance.GetQuest(questToGive?.questId);
        if(currentQ != null && currentQ.isActive && !currentQ.isCompleted)
        {
            if(currentQ.isGoalReached)
            {
                QuestManager.Instance.CompleteQuest(questToGive.questId);
                dialogueLines = dialogueAfter;
            }
            else
            {
                dialogueLines = dialogueDuring;
            }
        }
        else
        {
            bool questDone = !string.IsNullOrEmpty(questToGive?.questId) && QuestManager.Instance.IsCompleted(questToGive.questId);
            if(!questDone)
                dialogueLines = dialogueBefore;
        }

        if(dialogueLines != null && dialogueLines.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(dialogueLines);
            hasPlayed = true;
            if(!string.IsNullOrEmpty(questToCompleteOnTalk))
                QuestManager.Instance.CompleteQuest(questToCompleteOnTalk);
            if(activateAfterTalk != null)
                foreach(GameObject go in activateAfterTalk) if(go != null) go.SetActive(true);
        }
    }

    // void OnTriggerExit(Collider other)
    // {
    //     if(other.CompareTag("Player"))
    //     {
    //         Debug.Log("Player bước ra");
    //     }
    // }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
