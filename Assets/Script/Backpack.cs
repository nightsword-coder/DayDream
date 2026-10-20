using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Backpack : MonoBehaviour
{
    [FormerlySerializedAs("items")]
    public Transform itemRoot;
    public List<Book> items = new List<Book>();

    readonly List<Image> _slots = new List<Image>();

    void Awake()
    {
        CollectSlots();
    }

    void CollectSlots()
    {
        if (_slots.Count > 0 || itemRoot == null)
            return;

        for (int r = 0; r < itemRoot.childCount; r++)
        {
            Transform row = itemRoot.GetChild(r);
            for (int i = 0; i < row.childCount; i++)
            {
                Image image = row.GetChild(i).GetComponent<Image>();
                if (image == null)
                    continue;
                image.gameObject.SetActive(false);
                _slots.Add(image);
            }
        }
    }

    public void Place(Book book)
    {
        if (book == null)
            return;

        CollectSlots();
        if (itemRoot == null)
        {
            Debug.LogWarning("Backpack 的 Item Root 是空的，把 backpackpanel/items 拖上去。");
            return;
        }
        if (items.Count >= _slots.Count)
        {
            Debug.LogWarning("Backpack 没有找到格子。Item Root 要拖 backpackpanel/items，每个格子自身要有 Image。");
            return;
        }

        Image slot = _slots[items.Count];
        slot.sprite = book.sprite;
        slot.enabled = true;
        slot.gameObject.SetActive(true);
        items.Add(book);
    }
}
