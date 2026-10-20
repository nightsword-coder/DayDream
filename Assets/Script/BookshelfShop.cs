using UnityEngine;

/// <summary>
/// 挂在 bookshelf 上。玩家靠近时打开 ShopPanel，走开后关闭。
/// 面板被手动关掉后，要先离开范围再进入才会重新打开。
/// </summary>
public class BookshelfShop : MonoBehaviour
{
    public RoleControl player;
    public float openRange = 2.5f;
    public GameObject shopPanel;

    bool _open;
    bool _waitLeave;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<RoleControl>();
        if (shopPanel == null)
            shopPanel = FindInScene("ShopPanel");
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    void Update()
    {
        if (player == null || shopPanel == null)
            return;

        bool near = Vector2.Distance(player.transform.position, transform.position) <= openRange;
        if (!near)
        {
            _waitLeave = false;
            if (_open)
            {
                _open = false;
                shopPanel.SetActive(false);
            }
            return;
        }

        if (_waitLeave)
            return;

        if (_open && !shopPanel.activeSelf)
        {
            _open = false;
            _waitLeave = true;
            return;
        }

        if (!_open)
        {
            _open = true;
            shopPanel.SetActive(true);
        }
    }

    static GameObject FindInScene(string objectName)
    {
        GameObject active = GameObject.Find(objectName);
        if (active != null)
            return active;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Transform[] children = canvases[i].GetComponentsInChildren<Transform>(true);
            for (int c = 0; c < children.Length; c++)
            {
                if (children[c].name == objectName)
                    return children[c].gameObject;
            }
        }

        return null;
    }
}
