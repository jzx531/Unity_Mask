using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueRunner : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private DialogueCsvLoader loader;

    [Header("Session Switching")]
    [SerializeField] private ChatSessionManager sessionManager; // 你已有的切群聊器（负责切 Content/ChoicePanel）
    [SerializeField] private int groupCount = 4;

    [Header("UI")]
    [SerializeField] private ScrollRect messageScrollRect;
    [SerializeField] private TMP_InputField inputField;

    [Header("Prefabs")]
    [SerializeField] private GameObject leftBubblePrefab;
    [SerializeField] private GameObject rightBubblePrefab;
    [SerializeField] private GameObject daySeparatorPrefab;
    [SerializeField] private GameObject choiceBubblePrefab;

    [Header("Avatars (1..7)")]
    [SerializeField] private Sprite[] avatarSprites; // index 1..7 用（0留空）

    DialogueIndex _index;
    GlobalState _global = new GlobalState();
    Dictionary<int, ChatSessionState> _states = new Dictionary<int, ChatSessionState>();

    void Start()
    {
        var rows = loader.Load();
        _index = new DialogueIndex(rows);

        // 初始化4个群聊 state（群聊ID按你的表：1..4）
        for (int gid = 1; gid <= groupCount; gid++)
        {
            var st = new ChatSessionState
            {
                GroupId = gid,
                CurrentOrder = _index.GetFirstOrder(gid),
                LastShownDay = 0,
                WaitingChoice = false
            };
            _states[gid] = st;
        }

        // 默认进入当前群聊
        EnterCurrentGroup();
    }

    // 在 ChatSessionManager.SwitchTo 后调用它
    public void OnGroupSwitched()
    {
        EnterCurrentGroup();
    }

    void EnterCurrentGroup()
    {
        int gid = sessionManager.CurrentIndex + 1; // 你的按钮 index 0..3 -> groupId 1..4
        var st = _states[gid];

        // 如果该群聊 Content 为空（首次进入），自动从头跑一遍；否则只恢复选项面板
        // 这里用一个简化判定：如果 st.LastShownDay==0 说明还没播放过
        if (st.LastShownDay == 0)
        {
            ContinueAuto(st);
        }
        else
        {
            // 已经有历史：如果当时在等待选项，就把选项重新渲染出来
            if (st.WaitingChoice)
                RenderChoices(st);
        }

        ScrollToBottom();
    }

    void ContinueAuto(ChatSessionState st)
    {
        st.WaitingChoice = false;
        st.CurrentChoiceOrders.Clear();

        while (true)
        {
            if (!_index.TryGet(st.GroupId, st.CurrentOrder, out var row))
                return; // 没有更多内容

            // 1) 天数变化 -> 插入“第X天”
            if (row.Day != st.LastShownDay)
            {
                st.LastShownDay = row.Day;
                SpawnDaySeparator($"第{row.Day}天");
            }

            // 2) 如果这一行是“玩家选项”，说明我们走错了（正常不会自动播放到选项行）
            if (row.IsPlayerChoice)
                return;

            // 3) 播放该行（左气泡：发言人 1..7）
            SpawnLeft(row);

            // 4) 如果该行引出选项 -> 生成选项并停止自动播放
            if (row.TriggerChoices == 1)
            {
                st.WaitingChoice = true;
                CollectChoicesAfter(st, row.Order);
                RenderChoices(st);
                return;
            }

            // 5) 否则按“导向发言的次序”前进
            st.CurrentOrder = row.NextOrder;
        }
    }

    void CollectChoicesAfter(ChatSessionState st, int triggerOrder)
    {
        st.CurrentChoiceOrders.Clear();

        int cursor = triggerOrder + 1;
        for (int k = 0; k < 20; k++)
        {
            if (!_index.TryGet(st.GroupId, cursor, out var r)) break;
            if (!r.IsPlayerChoice) break;

            st.CurrentChoiceOrders.Add(cursor);
            cursor++;
        }
    }


    void RenderChoices(ChatSessionState st)
    {
        var choicePanel = sessionManager.GetCurrentChoicePanel();
        if (!choicePanel) return;

        // 清空旧的
        for (int i = choicePanel.childCount - 1; i >= 0; i--)
            Destroy(choicePanel.GetChild(i).gameObject);

        choicePanel.gameObject.SetActive(true);

        foreach (var order in st.CurrentChoiceOrders)
        {
            if (!_index.TryGet(st.GroupId, order, out var r)) continue;

            var go = Instantiate(choiceBubblePrefab, choicePanel);

            var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp) tmp.text = r.Content;

            var btn = go.GetComponentInChildren<Button>(true);
            if (!btn) btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                int captured = order;
                btn.onClick.AddListener(() => PickChoice(st, captured));
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    void PickChoice(ChatSessionState st, int choiceOrder)
    {
        if (!_index.TryGet(st.GroupId, choiceOrder, out var choiceRow)) return;

        // 1) 玩家选项作为右气泡发送
        SpawnRight(choiceRow.Content);

        // 2) 全局变量累加
        _global.Contradiction += choiceRow.ContradictionDelta;
        _global.Suspicion += choiceRow.SuspicionDelta;

        // 3) 跳转到导向发言的次序
        st.CurrentOrder = choiceRow.NextOrder;

        // 4) 关闭选项并继续自动播放
        st.WaitingChoice = false;
        st.CurrentChoiceOrders.Clear();

        var choicePanel = sessionManager.GetCurrentChoicePanel();
        if (choicePanel) choicePanel.gameObject.SetActive(false);

        ContinueAuto(st);
    }

    // ====== UI Spawn ======

    RectTransform CurrentContent() => sessionManager.GetCurrentContent();

    void SpawnLeft(DialogueRow row)
    {
        var content = CurrentContent();
        if (!content) return;

        var go = Instantiate(leftBubblePrefab, content);

        // 文本
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp) tmp.text = row.Content;

        // 头像（如果你的 LeftBubble prefab 有一个 Image 组件作为头像）
        var images = go.GetComponentsInChildren<Image>(true);
        // 建议你在 prefab 里给头像 Image 单独挂脚本/命名，这里先用“找第一个非背景Image”的简化方式
        foreach (var img in images)
        {
            if (img.gameObject.name.ToLower().Contains("avatar"))
            {
                int idx = int.TryParse(row.Speaker, out var v) ? v : 0;
                if (idx >= 0 && idx < avatarSprites.Length) img.sprite = avatarSprites[idx];
                break;
            }
        }

        ScrollToBottom();
    }

    void SpawnRight(string msg)
    {
        var content = CurrentContent();
        if (!content) return;

        var go = Instantiate(rightBubblePrefab, content);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp) tmp.text = msg;

        ScrollToBottom();
    }

    void SpawnDaySeparator(string msg)
    {
        var content = CurrentContent();
        if (!content) return;

        var go = Instantiate(daySeparatorPrefab, content);
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
