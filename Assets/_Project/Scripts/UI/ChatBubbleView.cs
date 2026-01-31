using TMPro;
using UnityEngine;

public class ChatBubbleView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    public void SetMessage(string msg)
    {
        if (!messageText)
        {
            Debug.LogError("ChatBubbleView: messageText is not assigned.");
            return;
        }
        messageText.text = msg;
    }

    public TextMeshProUGUI MessageText => messageText;
}
