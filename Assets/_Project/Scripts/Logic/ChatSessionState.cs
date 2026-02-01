using System.Collections.Generic;

public class ChatSessionState
{
    public int GroupId;

    // 当前播放到哪一条（order）
    public int CurrentOrder;

    // 上一次显示过的天数，用于插入“第X天”
    public int LastShownDay;

    // 是否正在等待玩家选项（ChoicePanel 显示中）
    public bool WaitingChoice;

    // 当前选项集合（A/B/C）对应的 order
    public List<int> CurrentChoiceOrders = new List<int>();

    // 全局变量（也可以放到 GlobalState，但你说是全局，后面会放 GlobalState）
}
