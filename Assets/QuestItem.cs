using UnityEngine;
using UnityEngine.InputSystem;

public class QuestItem : MonoBehaviour
{
    [SerializeField] private string questId;
    [SerializeField] private string itemName = "Vật phẩm";
    public static QuestItem Instance;
    private bool isPlayerInside = false;
    void Awake()
    {
        Instance = this;
    }
    void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player")) return;
        Quest q = QuestManager.Instance.GetQuest(questId);
        if(q != null && q.isActive && !q.isGoalReached)
        {
            QuestUI.Instance.ShowNotification("Bấm [E] để nhặt" + itemName);
            isPlayerInside = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!isPlayerInside || !Keyboard.current.eKey.wasPressedThisFrame) return;
        Quest q = QuestManager.Instance.GetQuest(questId);
        if(q != null && q.isActive && !q.isCompleted)
        {
            q.isGoalReached = true;
            QuestUI.Instance.ShowNotification("Đã nhặt " + itemName + " Quay lại gặp anh Ộ");
            QuestUI.Instance.UpdateUI();
            gameObject.SetActive(false);
        }
    }
}
