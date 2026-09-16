using UnityEngine;

public class GoalZone : MonoBehaviour
{
    [SerializeField] private string questId; // ID của quest cần hoàn thành

    void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player")) return;

        // Chỉ hoàn thành nếu quest đang active
        Quest q = QuestManager.Instance.GetQuest(questId);
        if(q == null || !q.isActive || q.isCompleted || !q.isGoalReached) return;

        QuestManager.Instance.CompleteQuest(questId);

        // Tắt GoalZone sau khi đã trigger (không trigger lại)
        gameObject.SetActive(false);
    }
}
