using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ChatBubbleTextWidthClamp : MonoBehaviour
{
    public float maxWidth = 520f;   // 固定最大宽度（像素）
    public float minWidth = 0f;

    TextMeshProUGUI tmp;
    RectTransform rt;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        rt = GetComponent<RectTransform>();

        tmp.textWrappingMode = TextWrappingModes.Normal;
    }


    void LateUpdate()
    {
        // 获取文本在“不换行情况下”需要的宽度
        float preferred =
            tmp.GetPreferredValues(tmp.text, Mathf.Infinity, Mathf.Infinity).x;

        float target = Mathf.Clamp(preferred, minWidth, maxWidth);

        // 直接写 RectTransform 宽度
        if (Mathf.Abs(rt.sizeDelta.x - target) > 0.1f)
        {
            rt.sizeDelta = new Vector2(target, rt.sizeDelta.y);
        }
    }
}
