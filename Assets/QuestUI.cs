using UnityEngine;
using TMPro;
using System.Collections;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance;
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI questDescText;
    [SerializeField] private TextMeshProUGUI questStatusText;
    [SerializeField] private GameObject questNotificationPanel;
    [SerializeField] private TextMeshProUGUI questNotificationText;

    void Awake()
    {
        Instance = this;
        questPanel.SetActive(false);
    }
    // Hàm tổng quát — hiện bất kỳ thông báo nào
    public void ShowNotification(string message)
    {
        questPanel.SetActive(false);
        questNotificationText.text = message;
        questNotificationPanel.SetActive(true);
        StartCoroutine(HideQuestNotification());
    }
    // Hàm nhận quest — thêm prefix
    public void ShowQuestNotification(string questName)
    {
        ShowNotification("Nhiệm vụ mới: " + questName);
    }
    IEnumerator HideQuestNotification()
    {
        yield return new WaitForSeconds(3f);
        questNotificationPanel.SetActive(false);
        UpdateUI();
    }
    public void ShowQuestComplete(string questName)
    {
        ShowNotification("Hoàn thành nhiệm vụ: " + questName);
    }
    public void UpdateUI()
    {
        Quest activeQuest = null;
        foreach(Quest q in QuestManager.Instance.GetActiveQuests())
        {
            if(q.isActive && !q.isCompleted)
            {
                activeQuest = q;
                break;
            }
        }
        if(activeQuest == null)
        {
            questPanel.SetActive(false);
            return;
        }
        questPanel.SetActive(true);
        questNameText.text = activeQuest.questName;
        questDescText.text = activeQuest.description;
        questStatusText.text = activeQuest.isCompleted ? "[Xong] Đã hoàn thành" : "[...] Đang thực hiện";
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
