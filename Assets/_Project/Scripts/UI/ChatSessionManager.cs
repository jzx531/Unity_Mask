using UnityEngine;
using UnityEngine.UI;

public class ChatSessionManager : MonoBehaviour
{
    [Header("Left Panel Buttons (4)")]
    [SerializeField] private Button[] chatButtons; // 4¸ö°´Å¥

    [Header("Per-Chat Containers (4)")]
    [SerializeField] private RectTransform[] chatContents; // Content_Group1..4
    [SerializeField] private RectTransform[] choicePanels; // ChoicePanel_Group1..4
    [SerializeField] private ChatDialogueControllerMulti dialogueController;
    [SerializeField] private DialogueRunner dialogueRunner;



    public int CurrentIndex { get; private set; } = 0;

    void Start()
    {
        for (int i = 0; i < chatButtons.Length; i++)
        {
            int idx = i;
            chatButtons[i].onClick.AddListener(() => SwitchTo(idx));
        }

        SwitchTo(0);
    }

    public void SwitchTo(int index)
    {
        if (index < 0 || index >= chatContents.Length) return;

        // ÏÔÒþÇÐ»»
        for (int i = 0; i < chatContents.Length; i++)
            if (chatContents[i]) chatContents[i].gameObject.SetActive(i == index);

        for (int i = 0; i < choicePanels.Length; i++)
            if (choicePanels[i]) choicePanels[i].gameObject.SetActive(i == index);

        if (dialogueController) dialogueController.OnChatSwitched();

        if (dialogueRunner) dialogueRunner.OnGroupSwitched();


        CurrentIndex = index;
    }

    public RectTransform GetCurrentContent()
        => (CurrentIndex >= 0 && CurrentIndex < chatContents.Length) ? chatContents[CurrentIndex] : null;

    public RectTransform GetCurrentChoicePanel()
        => (CurrentIndex >= 0 && CurrentIndex < choicePanels.Length) ? choicePanels[CurrentIndex] : null;
}
