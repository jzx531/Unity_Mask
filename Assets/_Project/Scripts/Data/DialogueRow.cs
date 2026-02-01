using System;

[Serializable]
public class DialogueRow
{
    public int Day;
    public int GroupId;
    public int Order;

    // "1/2/3/4/5/6/7" 或 "A/B/C"
    public string Speaker;

    public string Content;

    public int ContradictionDelta;
    public int SuspicionDelta;

    public int TriggerChoices;   // 是否引出选项
    public bool IsPlayerChoice;  // 是否为玩家选项
    public int NextOrder;        // 导向发言的次序
}

