using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatDialogueController : MonoBehaviour
{
    [Header("Existing Chat UI")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private ScrollRect messageScrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject leftBubblePrefab;
    [SerializeField] private GameObject rightBubblePrefab;

    [Header("Choice UI")]
    [SerializeField] private RectTransform choicePanel;      // Step 3.1 的 ChoicePanel
    [SerializeField] private GameObject choiceBubblePrefab;  // Step 3.2 的 ChoiceBubble.prefab
    [SerializeField] private int maxChoicesToShow = 3;

    // ====== Dialogue Model ======
    [Serializable]
    public class Choice
    {
        public string text;        // 选项文字（会自动填入输入框并发送）
        public string nextNodeId;  // 选完跳到哪个节点
    }

    [Serializable]
    public class Node
    {
        public string id;
        public string npcMessage;      // 左气泡先说的话（进入节点时发）
        public List<Choice> choices;   // 该节点的三选项（或少于3）
    }

    Dictionary<string, Node> _nodes;
    Node _current;

    void Start()
    {
        BuildDemoDialogue();
        EnterNode("start");
    }

    // 你后续可以把这些数据换成 ScriptableObject/JSON
    void BuildDemoDialogue()
    {
        _nodes = new Dictionary<string, Node>();

        _nodes["start"] = new Node
        {
            id = "start",
            npcMessage = "（群聊）大家好！今晚吃什么？",
            choices = new List<Choice>
            {
                new Choice{ text="火锅！", nextNodeId="hotpot"},
                new Choice{ text="烧烤吧", nextNodeId="bbq"},
                new Choice{ text="随便都行，你们定", nextNodeId="whatever"},
            }
        };

        _nodes["hotpot"] = new Node
        {
            id = "hotpot",
            npcMessage = "好！那就火锅。你想吃麻辣还是清汤？",
            choices = new List<Choice>
            {
                new Choice{ text="麻辣！越辣越好", nextNodeId="hotpot_spicy"},
                new Choice{ text="清汤，我怕辣", nextNodeId="hotpot_clear"},
                new Choice{ text="鸳鸯锅", nextNodeId="hotpot_dual"},
            }
        };

        _nodes["bbq"] = new Node
        {
            id = "bbq",
            npcMessage = "烧烤安排！你想吃哪家？",
            choices = new List<Choice>
            {
                new Choice{ text="公司附近那家", nextNodeId="bbq_near"},
                new Choice{ text="网红店试试", nextNodeId="bbq_hot"},
                new Choice{ text="我都行", nextNodeId="bbq_any"},
            }
        };

        _nodes["whatever"] = new Node
        {
            id = "whatever",
            npcMessage = "别‘随便’啦😂 你至少给个方向：辣/不辣？",
            choices = new List<Choice>
            {
                new Choice{ text="要辣的", nextNodeId="hotpot"},
                new Choice{ text="不辣的", nextNodeId="hotpot_clear"},
                new Choice{ text="你们投票吧", nextNodeId="vote"},
            }
        };

        _nodes["vote"] = new Node
        {
            id = "vote",
            npcMessage = "行，那我发个投票～（此处可扩展投票UI）",
            choices = new List<Choice>
            {
                new Choice{ text="我投火锅", nextNodeId="hotpot"},
                new Choice{ text="我投烧烤", nextNodeId="bbq"},
                new Choice{ text="我投其他", nextNodeId="other"},
            }
        };

        _nodes["other"] = new Node
        {
            id = "other",
            npcMessage = "那你说说想吃啥？（这里可以改成自由输入继续对话）",
            choices = new List<Choice>
            {
                new Choice{ text="日料", nextNodeId="end"},
                new Choice{ text="披萨", nextNodeId="end"},
                new Choice{ text="面", nextNodeId="end"},
            }
        };

        // 结束节点（无选项）
        _nodes["end"] = new Node
        {
            id = "end",
            npcMessage = "OK！",
            choices = new List<Choice>()
        };
    }

    void EnterNode(string nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out _current))
        {
            Debug.LogError($"Node not found: {nodeId}");
            return;
        }

        // 1) 先发左气泡（群聊消息）
        if (!string.IsNullOrEmpty(_current.npcMessage))
            SpawnLeft(_current.npcMessage);

        // 2) 再显示三条右气泡选项
        ShowChoices(_current.choices);
    }

    void ShowChoices(List<Choice> choices)
    {
        // 清空旧选项
        for (int i = choicePanel.childCount - 1; i >= 0; i--)
            Destroy(choicePanel.GetChild(i).gameObject);

        if (choices == null || choices.Count == 0)
        {
            choicePanel.gameObject.SetActive(false);
            return;
        }

        choicePanel.gameObject.SetActive(true);

        int count = Mathf.Min(maxChoicesToShow, choices.Count);
        for (int i = 0; i < count; i++)
        {
            var choice = choices[i];
            var go = Instantiate(choiceBubblePrefab, choicePanel);

            // 设置文字
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp) tmp.text = choice.text;

            // 绑定点击
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
        // 1) 选项文字自动输入到输入框
        inputField.text = choice.text;

        // 2) 自动发送成右气泡
        SpawnRight(choice.text);

        // 3) 清空输入框并保持焦点
        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();

        // 4) 隐藏选项（防止重复点）
        choicePanel.gameObject.SetActive(false);

        // 5) 进入下一节点：弹出对应左气泡 + 新的三选项
        if (!string.IsNullOrEmpty(choice.nextNodeId))
            EnterNode(choice.nextNodeId);
    }

    void SpawnLeft(string msg)
    {
        var go = Instantiate(leftBubblePrefab, content);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp) tmp.text = msg;
        ScrollToBottom();
    }

    void SpawnRight(string msg)
    {
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
}
