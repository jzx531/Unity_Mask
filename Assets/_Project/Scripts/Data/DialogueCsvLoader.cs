using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public class DialogueCsvLoader : MonoBehaviour
{
    [SerializeField] private TextAsset csvAsset;

    public List<DialogueRow> Load()
    {
        if (!csvAsset)
        {
            Debug.LogError("DialogueCsvLoader: csvAsset not assigned.");
            return new List<DialogueRow>();
        }

        var lines = csvAsset.text.Replace("\r\n", "\n").Split('\n');
        if (lines.Length <= 1) return new List<DialogueRow>();

        // 跳过表头
        var rows = new List<DialogueRow>(lines.Length - 1);

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = SplitCsvLine(line);
            if (cols.Count < 10) continue;

            // 对应你的列顺序：
            // 0 Day, 1 GroupId, 2 Order, 3 Speaker, 4 Content,
            // 5 contradiction, 6 suspicion, 7 trigger, 8 isChoice, 9 nextOrder
            var r = new DialogueRow
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

            rows.Add(r);
        }

        return rows;
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

    // 支持引号的 CSV 拆分
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
