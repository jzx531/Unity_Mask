using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatSendController : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private ScrollRect messageScrollRect;
    [SerializeField] private RectTransform content;

    [Header("Bubble Prefabs")]
    [SerializeField] private GameObject rightBubblePrefab;

    void Awake()
    {
        if (inputField != null)
            inputField.onSubmit.AddListener(_ => TrySend());
    }

    void OnDestroy()
    {
        if (inputField != null)
            inputField.onSubmit.RemoveAllListeners();
    }

    void Update()
    {
        // 兜底：确保按回车能发送（某些输入设置下 onSubmit 可能不触发）
        if (inputField == null || !inputField.isFocused) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            TrySend();
    }

    private void TrySend()
    {
        if (inputField == null || rightBubblePrefab == null || content == null) return;

        var text = inputField.text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var bubble = Instantiate(rightBubblePrefab, content);

        // 找到气泡里的 TextMeshProUGUI（你气泡里 MessageText 就是它）
        var tmp = bubble.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) tmp.text = text;

        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();

        // 布局刷新 + 滚到底
        Canvas.ForceUpdateCanvases();
        if (messageScrollRect != null)
            messageScrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}
