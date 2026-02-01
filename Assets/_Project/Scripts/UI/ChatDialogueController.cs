using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatDialogueControllerMulti : MonoBehaviour
{
    [Header("Session + UI")]
    [SerializeField] private ChatSessionManager session;   // 负责切 Content/ChoicePanel
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private ScrollRect messageScrollRect;

    [Header("CSV")]
    [SerializeField] private TextAsset csvAsset;
    [Tooltip("你的群聊数量（左侧4个群）")]
    [SerializeField] private int groupCount = 4;

    [Header("Prefabs")]
    [SerializeField] private GameObject leftBubblePrefab;
    [SerializeField] private GameObject rightBubblePrefab;
    [SerializeField] private GameObject choiceBubblePrefab;
    [SerializeField] private GameObject daySeparatorPrefab; // 可选：显示“第X天”

    [Header("Options")]
    [Range(1, 6)] public int maxChoicesToShow = 3;

    // ====== Data Model (CSV Row) ======
    class Row
    {
        public int Day;
        public int GroupId;
        public int Order;
        public string Speaker; // 1..7 or A/B/C
        public string Content;
        public int ContradictionDelta;
        public int SuspicionDelta;
        public int TriggerChoices;   // 是否引出选项
        public bool IsPlayerChoice;  // 是否为玩家选项
        public int NextOrder;        // 导向发言的次序
    }

    // groupId -> (order -> row)
    Dictionary<int, Dictionary<int, Row>> _rowsByGroup = new Dictionary<int, Dictionary<int, Row>>();
    Dictionary<int, int> _firstOrderByGroup = new Dictionary<int, int>();

    // 全局变量
    int _globalSuspicion = 0;
    int _globalContradiction = 0;

    // 每群一个状态
    class ChatState
    {
        public bool started;
        public int currentOrder;     // 当前指针
        public int lastShownDay;     // 上次显示的天数（用于插入 Day 分割线）
        public bool waitingChoice;
        public List<int> currentChoiceOrders = new List<int>(); // A/B/C 行的 order 列表
    }

    ChatState[] _states;

    void Start()
    {
        BuildIndexFromCsv();

        _states = new ChatState[groupCount];
        for (int i = 0; i < _states.Length; i++)
            _states[i] = new ChatState();

        // 默认进入当前群
        OnChatSwitched();
    }

    // 让 ChatSessionManager.SwitchTo() 调用这个（你之前已经在调用）
    public void OnChatSwitched()
    {
        int idx = session.CurrentIndex;      // 0..3
        int groupId = idx + 1;              // 1..4 (与你表一致)

        EnsureStarted(idx, groupId);

        // 如果该群离开前在等待选项，切回来要重新画出来
        if (_states[idx].waitingChoice)
            RenderChoices(idx, groupId);
        else
        {
            // 不在等选项：确保当前 choicePanel 关闭
            var cp = session.GetCurrentChoicePanel();
            if (cp) cp.gameObject.SetActive(false);
        }

        ScrollToBottom();
    }

    void EnsureStarted(int chatIndex, int groupId)
    {
        var st = _states[chatIndex];
        if (st.started) return;

        st.started = true;
        st.lastShownDay = 0;
        st.waitingChoice = false;
        st.currentChoiceOrders.Clear();

        st.currentOrder = _firstOrderByGroup.TryGetValue(groupId, out var first) ? first : -1;

        // 首次进入该群：自动播放直到出现选项
        ContinueAuto(chatIndex, groupId);
    }

    // ====== Core Play Loop ======
    void ContinueAuto(int chatIndex, int groupId)
    {
        var st = _states[chatIndex];
        st.waitingChoice = false;
        st.currentChoiceOrders.Clear();

        while (true)
        {
            if (st.currentOrder <= 0) return;
            if (!TryGetRow(groupId, st.currentOrder, out var row)) return;

            // 1) 天数变化 -> 插入分割线（可选）
            if (daySeparatorPrefab != null && row.Day != st.lastShownDay)
            {
                st.lastShownDay = row.Day;
                SpawnDaySeparator($"第{row.Day}天");
            }
            else
            {
                st.lastShownDay = row.Day;
            }

            // 2) 自动播放阶段不应该播到选项行
            if (row.IsPlayerChoice)
                return;

            // 3) 播放 NPC 行（左气泡）
            SpawnLeft(row.Content);

            // 4) 如果该行引出选项 -> 收集紧随其后的 A/B/C 行，显示选项并停住
            if (row.TriggerChoices == 1)
            {
                st.waitingChoice = true;
                CollectChoicesAfter(chatIndex, groupId, row.Order);
                RenderChoices(chatIndex, groupId);
                return;
            }

            // 5) 否则按 NextOrder 前进
            st.currentOrder = row.NextOrder;
        }
    }

    void CollectChoicesAfter(int chatIndex, int groupId, int triggerOrder)
    {
        var st = _states[chatIndex];
        st.currentChoiceOrders.Clear();

        // 你的表设计：选项行通常紧跟在触发行后面（Order+1, +2, +3 ...）
        int cursor = triggerOrder + 1;

        for (int k = 0; k < 20; k++)
        {
            if (!TryGetRow(groupId, cursor, out var r)) break;
            if (!r.IsPlayerChoice) break;

            st.currentChoiceOrders.Add(cursor);
            cursor++;
        }
    }

    void RenderChoices(int chatIndex, int groupId)
    {
        var choicePanel = session.GetCurrentChoicePanel();
        if (!choicePanel) return;

        // 清空旧选项
        for (int i = choicePanel.childCount - 1; i >= 0; i--)
            Destroy(choicePanel.GetChild(i).gameObject);

        var st = _states[chatIndex];
        if (st.currentChoiceOrders.Count == 0)
        {
            choicePanel.gameObject.SetActive(false);
            st.waitingChoice = false;
            return;
        }

        choicePanel.gameObject.SetActive(true);

        int count = Mathf.Min(maxChoicesToShow, st.currentChoiceOrders.Count);
        for (int i = 0; i < count; i++)
        {
            int order = st.currentChoiceOrders[i];
            if (!TryGetRow(groupId, order, out var r)) continue;

            var go = Instantiate(choiceBubblePrefab, choicePanel);

            var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp) tmp.text = r.Content;

            var btn = go.GetComponentInChildren<Button>(true);
            if (!btn) btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                int capturedOrder = order;
                btn.onClick.AddListener(() => PickChoice(chatIndex, groupId, capturedOrder));
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    void PickChoice(int chatIndex, int groupId, int choiceOrder)
    {
        if (!TryGetRow(groupId, choiceOrder, out var choiceRow)) return;

        // 1) 自动“发出”右气泡（并非在气泡里输入）
        SpawnRight(choiceRow.Content);

        // 2) 累加全局变量
        _globalContradiction += choiceRow.ContradictionDelta;
        _globalSuspicion += choiceRow.SuspicionDelta;

        // 你之后可以在 UI 上显示这两个值（调试用）
        Debug.Log($"[Global] Contradiction={_globalContradiction}, Suspicion={_globalSuspicion}");

        // 3) 关闭选项
        var cp = session.GetCurrentChoicePanel();
        if (cp) cp.gameObject.SetActive(false);

        // 4) 跳转到 NextOrder 继续播放
        var st = _states[chatIndex];
        st.waitingChoice = false;
        st.currentChoiceOrders.Clear();
        st.currentOrder = choiceRow.NextOrder;

        ContinueAuto(chatIndex, groupId);
    }

    // ====== UI Spawning ======
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

        // 输入框也可以同步显示再清空（可选）
        if (inputField)
        {
            inputField.text = "";
            inputField.ActivateInputField();
            inputField.Select();
        }

        ScrollToBottom();
    }

    void SpawnDaySeparator(string msg)
    {
        if (daySeparatorPrefab == null) return;

        var content = session.GetCurrentContent();
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

    // ====== CSV Index ======
    bool TryGetRow(int groupId, int order, out Row row)
    {
        row = null;
        return _rowsByGroup.TryGetValue(groupId, out var map) && map.TryGetValue(order, out row);
    }

    void BuildIndexFromCsv()
    {
        _rowsByGroup.Clear();
        _firstOrderByGroup.Clear();

        if (!csvAsset)
        {
            Debug.LogError("ChatDialogueControllerMulti: csvAsset is not assigned.");
            return;
        }

        var lines = csvAsset.text.Replace("\r\n", "\n").Split('\n');
        if (lines.Length <= 1)
        {
            Debug.LogError("CSV has no data lines.");
            return;
        }

        // skip header
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = SplitCsvLine(line);
            if (cols.Count < 10) continue;

            var r = new Row
            {
                Day = ToInt(cols[0]),
                GroupId = ToInt(cols[1]),
                Order = ToInt(cols[2]),
                Speaker = cols[3].Trim(),
                Content = cols[4],
                ContradictionDelta = ToInt(cols[5]),
                SuspicionDelta = ToInt(cols[6]),
                TriggerChoices = ToInt(cols[7]),
                IsPlayerChoice = ToBool(cols[8]),
                NextOrder = ToInt(cols[9]),
            };

            if (!_rowsByGroup.TryGetValue(r.GroupId, out var map))
            {
                map = new Dictionary<int, Row>();
                _rowsByGroup[r.GroupId] = map;
            }

            map[r.Order] = r;

            if (!_firstOrderByGroup.ContainsKey(r.GroupId) || r.Order < _firstOrderByGroup[r.GroupId])
                _firstOrderByGroup[r.GroupId] = r.Order;
        }

        Debug.Log($"CSV loaded. Groups={_rowsByGroup.Count}");
    }

    static int ToInt(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
        if (float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f)) return (int)f;
        return 0;
    }

    static bool ToBool(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    // 简易 CSV 拆分（支持引号）
    static List<string> SplitCsvLine(string line)
    {
        var res = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                res.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        res.Add(sb.ToString());
        return res;
    }
}
