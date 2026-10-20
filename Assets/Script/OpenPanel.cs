using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在按钮上。点击后打开拖入的面板。
/// </summary>
public class OpenPanel : MonoBehaviour
{
    public GameObject panel;

    void Awake()
    {
        Button button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(Open);
    }

    public void Open()
    {
        if (panel != null)
            panel.SetActive(true);
    }
}
