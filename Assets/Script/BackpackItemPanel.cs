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

    Book _current;

    void Awake()
    {
        if (itemPanel != null)
            itemPanel.SetActive(false);

        if (backpack != null)
            backpack.BindSlotClicks(Show);
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;
        if (itemPanel == null || !itemPanel.activeSelf)
            return;

        if (!Inside(itemPanel.transform as RectTransform))
            itemPanel.SetActive(false);
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
        if (_current == null || _current.skills == null)
            return;

        if (AllSkill.Instance != null)
        {
            for (int i = 0; i < _current.skills.Count; i++)
                AllSkill.Instance.AddSkill(_current.skills[i]);
        }

        MarkLearned(_current.skills);
    }

    void ShowSkills(List<Skill> skills)
    {
        Transform content = TomeContent();
        if (content == null)
        {
            Debug.LogWarning("tomepanel not found");
            return;
        }

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

            bool learned = AllSkill.Instance != null && AllSkill.Instance.skills.Contains(skill);
            SetLearned(slot, learned);
        }
    }

    void MarkLearned(List<Skill> skills)
    {
        Transform content = TomeContent();
        if (content == null)
            return;

        int count = skills != null ? skills.Count : 0;
        for (int i = 0; i < content.childCount && i < count; i++)
        {
            if (skills[i] == null)
                continue;
            SetLearned(content.GetChild(i), true);
        }
    }

    static void SetLearned(Transform slot, bool on)
    {
        Transform learned = FindNamed(slot, "learned");
        if (learned != null)
            learned.gameObject.SetActive(on);
    }

    static Transform FindNamed(Transform root, string childName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return child;
            Transform nested = FindNamed(child, childName);
            if (nested != null)
                return nested;
        }
        return null;
    }

    Transform TomeContent()
    {
        GameObject panel = FindTomePanel();
        if (panel == null)
            return null;

        ScrollRect scroll = panel.GetComponentInChildren<ScrollRect>(true);
        if (scroll == null)
            return null;
        return scroll.content;
    }

    static Image FindImage(Transform slot)
    {
        Transform named = FindNamed(slot, "skillimage");
        if (named != null)
        {
            Image image = named.GetComponent<Image>();
            if (image != null)
                return image;
        }

        Image[] images = slot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].transform != slot && images[i].transform.name != "learned")
                return images[i];
        }
        return null;
    }

    static TMP_Text FindText(Transform slot)
    {
        Transform named = FindNamed(slot, "skilldescription");
        if (named != null)
        {
            TMP_Text namedText = named.GetComponent<TMP_Text>();
            if (namedText == null)
                namedText = named.GetComponentInChildren<TMP_Text>(true);
            if (namedText != null)
                return namedText;
        }

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
