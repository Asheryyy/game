using UnityEngine;

public class Trigger : MonoBehaviour
{
    [SerializeField] private Quest questToGive;
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private string requiredQuestId;
    [SerializeField] private DialogueLine[] dialogueBefore;
    [SerializeField] private DialogueLine[] dialogueDuring;
    [SerializeField] private DialogueLine[] dialogueAfter;    
    void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player") || DialogueManager.Instance.IsActive)
        {
            return;
        }
        if(!string.IsNullOrEmpty(requiredQuestId) && !QuestManager.Instance.IsCompleted(requiredQuestId))
        {
            Quest reqQ = QuestManager.Instance.GetQuest(requiredQuestId);
            string reqName = reqQ != null ? reqQ.questName : requiredQuestId;
            QuestUI.Instance.ShowNotification("Cần hoàn thành " + reqName + " trước");
            return;
        }
        Quest currentQ = QuestManager.Instance.GetQuest(questToGive.questId);
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
            bool questDone = QuestManager.Instance.IsCompleted(questToGive.questId);
            if(!questDone)
            {
                dialogueLines = dialogueBefore;
            }
        }
        if(dialogueLines != null && dialogueLines.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(dialogueLines);
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
