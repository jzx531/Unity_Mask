using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatDialogueControllerMulti : MonoBehaviour
{
    [Header("Session + UI")]
    [SerializeField] private ChatSessionManager session;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private ScrollRect messageScrollRect;

    [Header("Prefabs")]
    [SerializeField] private GameObject leftBubblePrefab;
    [SerializeField] private GameObject rightBubblePrefab;
    [SerializeField] private GameObject choiceBubblePrefab;

    [Header("Options")]
    [Range(0.1f, 1f)] public float maxChoicesToShow = 3;

    // ===== Dialogue Model =====
    [Serializable] public class Choice { public string text; public string nextNodeId; }
    [Serializable] public class Node { public string id; public string npcMessage; public List<Choice> choices; }

    Dictionary<string, Node> _nodes;

    // 每个群聊一个状态
    class ChatState
    {
        public string currentNodeId;
        public bool started;
    }

    ChatState[] _states;

    void Start()
    {
        BuildDemoDialogue();

        // 4个群聊（你可以改成 session.chatContents.Length 但字段是 private，先固定）
        _states = new ChatState[4];
        for (int i = 0; i < _states.Length; i++) _states[i] = new ChatState();

        // 初始群聊：进入并启动
        EnsureStarted(session.CurrentIndex);
        RenderChoicesForCurrentChat();
    }

    // 你需要在 ChatSessionManager.SwitchTo 之后调用它（下一步会教你怎么接）
    public void OnChatSwitched()
    {
        EnsureStarted(session.CurrentIndex);
        RenderChoicesForCurrentChat();
        ScrollToBottom();
    }

    void EnsureStarted(int chatIndex)
    {
        var st = _states[chatIndex];
        if (st.started) return;

        st.started = true;
        st.currentNodeId = "start";

        // 第一次进入该群聊，发开场左气泡并出选项
        EnterNode(chatIndex, st.currentNodeId);
    }

    void EnterNode(int chatIndex, string nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            Debug.LogError($"Node not found: {nodeId}");
            return;
        }

        _states[chatIndex].currentNodeId = nodeId;

        if (!string.IsNullOrEmpty(node.npcMessage))
            SpawnLeft(node.npcMessage);

        ShowChoices(node.choices);
    }

    void ShowChoices(List<Choice> choices)
    {
        var choicePanel = session.GetCurrentChoicePanel();
        if (!choicePanel) return;

        // 清空旧选项
        for (int i = choicePanel.childCount - 1; i >= 0; i--)
            Destroy(choicePanel.GetChild(i).gameObject);

        if (choices == null || choices.Count == 0)
        {
            choicePanel.gameObject.SetActive(false);
            return;
        }

        choicePanel.gameObject.SetActive(true);

        int count = Mathf.Min((int)maxChoicesToShow, choices.Count);
        for (int i = 0; i < count; i++)
        {
            var choice = choices[i];
            var go = Instantiate(choiceBubblePrefab, choicePanel);

            var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp) tmp.text = choice.text;

            var btn = go.GetComponentInChildren<Button>(true);
            if (!btn) btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnChoiceClicked(choice));
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    void OnChoiceClicked(Choice choice)
    {
        // 自动填入 + 自动发送（右气泡）
        inputField.text = choice.text;
        SpawnRight(choice.text);
        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();

        // 隐藏当前选项，避免重复点
        var cp = session.GetCurrentChoicePanel();
        if (cp) cp.gameObject.SetActive(false);

        // 进入下一节点（在当前群聊）
        int idx = session.CurrentIndex;
        if (!string.IsNullOrEmpty(choice.nextNodeId))
            EnterNode(idx, choice.nextNodeId);
    }

    void RenderChoicesForCurrentChat()
    {
        int idx = session.CurrentIndex;
        var nodeId = _states[idx].currentNodeId;
        if (string.IsNullOrEmpty(nodeId)) return;

        if (_nodes.TryGetValue(nodeId, out var node))
            ShowChoices(node.choices);
    }

    void SpawnLeft(string msg)
    {
        var content = session.GetCurrentContent();
        if (!content) return;

        var go = Instantiate(leftBubblePrefab, content);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp) tmp.text = msg;
        ScrollToBottom();
    }

    void SpawnRight(string msg)
    {
        var content = session.GetCurrentContent();
        if (!content) return;

        var go = Instantiate(rightBubblePrefab, content);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp) tmp.text = msg;
        ScrollToBottom();
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (messageScrollRect) messageScrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

    void BuildDemoDialogue()
    {
        _nodes = new Dictionary<string, Node>();

        _nodes["start"] = new Node
        {
            id = "start",
            npcMessage = "（群聊）欢迎来到群聊 A：今晚安排？",
            choices = new List<Choice>
            {
                new Choice{ text="火锅", nextNodeId="hotpot"},
                new Choice{ text="烧烤", nextNodeId="bbq"},
                new Choice{ text="随便", nextNodeId="whatever"},
            }
        };

        _nodes["hotpot"] = new Node
        {
            id = "hotpot",
            npcMessage = "火锅OK！麻辣/清汤/鸳鸯？",
            choices = new List<Choice>
            {
                new Choice{ text="麻辣", nextNodeId="end"},
                new Choice{ text="清汤", nextNodeId="end"},
                new Choice{ text="鸳鸯", nextNodeId="end"},
            }
        };

        _nodes["bbq"] = new Node
        {
            id = "bbq",
            npcMessage = "烧烤走起！去哪家？",
            choices = new List<Choice>
            {
                new Choice{ text="公司附近", nextNodeId="end"},
                new Choice{ text="网红店", nextNodeId="end"},
                new Choice{ text="都行", nextNodeId="end"},
            }
        };

        _nodes["whatever"] = new Node
        {
            id = "whatever",
            npcMessage = "别随便😂 给个方向：辣/不辣？",
            choices = new List<Choice>
            {
                new Choice{ text="要辣", nextNodeId="hotpot"},
                new Choice{ text="不辣", nextNodeId="end"},
                new Choice{ text="投票吧", nextNodeId="end"},
            }
        };

        _nodes["end"] = new Node
        {
            id = "end",
            npcMessage = "OK，就这么定。",
            choices = new List<Choice>()
        };
    }
}
