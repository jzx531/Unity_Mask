using UnityEngine;
using TMPro;

public class textchange : MonoBehaviour
{
    [SerializeField] private TMP_Text newsText;

    public void OnButtonClick()
    {
        Debug.Log("Button Clicked");
        if (newsText != null)
        {
            newsText.text = "Hello World! " + Time.time;
        }
    }
}


