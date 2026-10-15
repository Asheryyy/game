using UnityEngine;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;
    private List<Quest> activeQuests = new List<Quest>();
    public List<Quest> GetActiveQuests() => activeQuests;
    void Awake()
    {
        Instance = this;
    }
    public void StartQuest(Quest quest)
    {
        if(GetQuest(quest.questId) != null) return;
        quest.isActive = true;
        quest.isCompleted = false;
        activeQuests.Add(quest);
        Debug.Log("StartQuest gọi: " + quest.questId);
        QuestUI.Instance.ShowQuestNotification(quest.questName);
        SaveSystem.RequestAutoSave(); // checkpoint mỗi khi nhận nhiệm vụ mới
    }
    public void CompleteQuest(string id)
    {
        Quest q = GetQuest(id);
        if(q == null)
        {
            Debug.LogWarning("CompleteQuest: chưa nhận quest '" + id + "' nên không hoàn thành được");
            return;
        }
        if(q.isCompleted) return;
        q.isCompleted = true;
        q.isActive = false;
        QuestUI.Instance.ShowQuestComplete(q.questName);
        QuestUI.Instance.UpdateUI();
    }
    // Khôi phục danh sách nhiệm vụ từ file save
    public void RestoreQuests(List<Quest> quests)
    {
        activeQuests.Clear();
        if (quests != null) activeQuests.AddRange(quests);
    }
    public Quest GetQuest(string id)
    {
        return activeQuests.Find(q => q.questId == id);
    }
    public bool IsCompleted(string id)
    {
        Quest q = GetQuest(id);
        return q != null && q.isCompleted;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
