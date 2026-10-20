using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackpackItemPanel : MonoBehaviour
{
    public Backpack backpack;
    public GameObject itemPanel;
    public Image itemImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText;

    readonly List<Image> _slots = new List<Image>();
    Book _current;

    void Awake()
    {
        if (itemPanel != null)
            itemPanel.SetActive(false);

        if (backpack == null || backpack.itemRoot == null)
            return;

        Transform items = backpack.itemRoot;
        for (int r = 0; r < items.childCount; r++)
        {
            Transform row = items.GetChild(r);
            for (int i = 0; i < row.childCount; i++)
            {
                Image image = row.GetChild(i).GetComponent<Image>();
                if (image != null)
                    _slots.Add(image);
            }
        }
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (itemPanel != null && itemPanel.activeSelf)
        {
            RectTransform panelRect = itemPanel.transform as RectTransform;
            if (!Inside(panelRect))
                itemPanel.SetActive(false);
            return;
        }

        int index = SlotUnderMouse();
        if (index >= 0)
            Show(index);
    }

    void Show(int index)
    {
        if (backpack == null || index >= backpack.items.Count)
            return;

        Book book = backpack.items[index];
        if (book == null || itemPanel == null)
            return;

        if (itemImage != null)
        {
            itemImage.sprite = book.sprite;
            itemImage.enabled = book.sprite != null;
        }
        if (nameText != null)
            nameText.text = book.bookName;
        if (descriptionText != null)
            descriptionText.text = book.description;
        itemPanel.SetActive(true);
        _current = book;
        ShowSkills(book.skills);
    }

    public void LearnAll()
    {
        if (_current == null || _current.skills == null || AllSkill.Instance == null)
            return;

        for (int i = 0; i < _current.skills.Count; i++)
            AllSkill.Instance.AddSkill(_current.skills[i]);
    }

    void ShowSkills(List<Skill> skills)
    {
        GameObject panel = FindTomePanel();
        if (panel == null)
        {
            Debug.LogWarning("tomepanel not found");
            return;
        }

        ScrollRect scroll = panel.GetComponentInChildren<ScrollRect>(true);
        if (scroll == null || scroll.content == null)
            return;

        Transform content = scroll.content;
        int count = skills != null ? skills.Count : 0;
        for (int i = 0; i < content.childCount; i++)
        {
            Transform slot = content.GetChild(i);
            bool hasSkill = i < count && skills[i] != null;
            slot.gameObject.SetActive(hasSkill);
            if (!hasSkill)
                continue;

            Skill skill = skills[i];
            Image image = FindImage(slot);
            if (image != null)
            {
                image.sprite = skill.image;
                image.enabled = skill.image != null;
            }

            TMP_Text text = FindText(slot);
            if (text != null)
                text.text = skill.description;
        }
    }

    static Image FindImage(Transform slot)
    {
        Image[] images = slot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].transform != slot)
                return images[i];
        }
        return null;
    }

    static TMP_Text FindText(Transform slot)
    {
        TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].transform != slot)
                return texts[i];
        }
        return null;
    }

    static GameObject FindTomePanel()
    {
        GameObject active = GameObject.Find("TomePanel");
        if (active != null)
            return active;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Transform[] children = canvases[i].GetComponentsInChildren<Transform>(true);
            for (int c = 0; c < children.Length; c++)
            {
                if (children[c].name == "TomePanel")
                    return children[c].gameObject;
            }
        }
        return null;
    }

    int SlotUnderMouse()
    {
        Camera camera = UiCamera();
        for (int i = 0; i < _slots.Count; i++)
        {
            Image slot = _slots[i];
            if (slot == null || !slot.gameObject.activeInHierarchy)
                continue;
            if (Inside(slot.rectTransform, camera))
                return i;
        }
        return -1;
    }

    bool Inside(RectTransform rect)
    {
        return Inside(rect, UiCamera());
    }

    bool Inside(RectTransform rect, Camera camera)
    {
        if (rect == null)
            return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, camera);
    }

    Camera UiCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return canvas.worldCamera;
        return null;
    }
}
