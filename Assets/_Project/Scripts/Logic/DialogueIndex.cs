using System.Collections.Generic;
using System.Linq;

public class DialogueIndex
{
    // groupId -> (order -> row)
    public readonly Dictionary<int, Dictionary<int, DialogueRow>> RowsByGroup
        = new Dictionary<int, Dictionary<int, DialogueRow>>();

    // groupId -> sorted orders
    public readonly Dictionary<int, List<int>> OrdersByGroup
        = new Dictionary<int, List<int>>();

    public DialogueIndex(List<DialogueRow> rows)
    {
        foreach (var r in rows)
        {
            if (!RowsByGroup.TryGetValue(r.GroupId, out var map))
            {
                map = new Dictionary<int, DialogueRow>();
                RowsByGroup[r.GroupId] = map;
            }
            map[r.Order] = r;
        }

        foreach (var kv in RowsByGroup)
            OrdersByGroup[kv.Key] = kv.Value.Keys.OrderBy(x => x).ToList();
    }

    public bool TryGet(int groupId, int order, out DialogueRow row)
    {
        row = null;
        return RowsByGroup.TryGetValue(groupId, out var map) && map.TryGetValue(order, out row);
    }

    public int GetFirstOrder(int groupId)
    {
        return OrdersByGroup.TryGetValue(groupId, out var list) && list.Count > 0 ? list[0] : -1;
    }
}
