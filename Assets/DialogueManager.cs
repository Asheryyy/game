using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    private DialogueLine[] lines;
    private int currentLine = 0;
    public bool IsActive {get; private set;}
    private Quest pendingQuest = null;
    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }
    public void StartDialogue(DialogueLine[] lines)
    {
        this.lines = lines;
        currentLine = 0;
        ShowLine();
        dialoguePanel.SetActive(true);
        IsActive = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void ShowLine()
    {
        nameText.text = lines[currentLine].speakerName;
        dialogueText.text = lines[currentLine].text;
        if(!string.IsNullOrEmpty(lines[currentLine].questToGive.questId))
        {
            pendingQuest = lines[currentLine].questToGive;
        }
    }
    public void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        IsActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if(pendingQuest != null && !string.IsNullOrEmpty(pendingQuest.questId) && QuestManager.Instance != null)
        {
            QuestManager.Instance.StartQuest(pendingQuest);
            pendingQuest = null;
        }
    }
    public void NextLine()
    {
        currentLine++;
        if(currentLine < lines.Length)
        {
            ShowLine();
        }   
        else
        {
            EndDialogue();
        }
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
