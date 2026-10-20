using UnityEngine;
using UnityEngine.UI;

public class OpenTomePanel : MonoBehaviour
{
    public GameObject tombPanel;

    void Awake()
    {
        Button button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(Open);
    }

    public void Open()
    {
        if (tombPanel != null)
            tombPanel.SetActive(true);
        if (transform.parent != null)
            transform.parent.gameObject.SetActive(false);
    }
}
