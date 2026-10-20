using UnityEngine;
using UnityEngine.UI;

public class CloseParentPanel : MonoBehaviour
{
    void Awake()
    {
        Button button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(Close);
    }

    public void Close()
    {
        if (transform.parent != null)
            transform.parent.gameObject.SetActive(false);
    }
}
