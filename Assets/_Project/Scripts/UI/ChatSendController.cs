using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatSendController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField inputField;

    [Header("Scrolling")]
    [SerializeField] private ScrollRect messageScrollRect;

    [Header("Session")]
    [SerializeField] private ChatSessionManager session;

    [Header("Prefabs")]
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
        if (!inputField || !inputField.isFocused) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            TrySend();
    }

    public void TrySend()
    {
        var text = inputField.text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var content = session ? session.GetCurrentContent() : null;
        if (!content)
        {
            Debug.LogError("No current content from session.");
            return;
        }

        var bubble = Instantiate(rightBubblePrefab, content);
        var tmp = bubble.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp) tmp.text = text;

        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();

        ScrollToBottom();
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (messageScrollRect) messageScrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}
