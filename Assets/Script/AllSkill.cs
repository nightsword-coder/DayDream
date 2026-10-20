using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AllSkill : MonoBehaviour
{
    public static AllSkill Instance { get; private set; }

    public List<Skill> skills = new List<Skill>();

    bool _panelWasOpen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        GameObject panel = FindInScene("SkillPanel");
        bool open = panel != null && panel.activeInHierarchy;
        if (open && !_panelWasOpen)
            FillSkillPanel();
        _panelWasOpen = open;
    }

    public void AddSkill(Skill skill)
    {
        if (skill == null || skills.Contains(skill))
            return;

        skills.Add(skill);
        if (_panelWasOpen)
            Refresh();
    }

    public void Refresh()
    {
        if (_panelWasOpen)
            FillSkillPanel();
    }

    void FillSkillPanel()
    {
        Transform content = FindAllSkillsContent();
        if (content == null || content.childCount == 0)
            return;

        var shown = new List<Skill>();
        for (int i = 0; i < skills.Count; i++)
        {
            Skill skill = skills[i];
            if (skill == null)
                continue;
            if (EquippedSkill.Instance != null && EquippedSkill.Instance.IsEquipped(skill))
                continue;
            shown.Add(skill);
        }

        Transform template = content.GetChild(0);
        while (content.childCount < shown.Count)
            Instantiate(template.gameObject, content);

        for (int i = 0; i < content.childCount; i++)
        {
            Transform slot = content.GetChild(i);
            if (i >= shown.Count)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            slot.gameObject.SetActive(true);
            Apply(slot, shown[i]);
            if (EquippedSkill.Instance != null)
                EquippedSkill.Instance.BindIcon(slot, shown[i], false, -1);
        }
    }

    static void Apply(Transform slot, Skill skill)
    {
        Transform imageObject = FindChild(slot, "skillimage");
        if (imageObject != null)
        {
            Image image = imageObject.GetComponent<Image>();
            if (image != null)
                image.sprite = skill.image;
        }

        SetNumber(slot, skill.mana, "manatext", "mana");
        SetNumber(slot, skill.power, "powertext", "power");
    }

    static void SetNumber(Transform slot, int value, params string[] childNames)
    {
        Transform child = null;
        for (int i = 0; i < childNames.Length; i++)
        {
            child = FindChild(slot, childNames[i]);
            if (child != null)
                break;
        }
        if (child == null)
            return;

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text == null)
            text = child.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = value.ToString();
    }

    static Transform FindAllSkillsContent()
    {
        GameObject panel = FindInScene("SkillPanel");
        if (panel == null)
            return null;

        Transform allSkills = FindChild(panel.transform, "allskills");
        if (allSkills == null)
            return null;

        ScrollRect scroll = allSkills.GetComponentInChildren<ScrollRect>(true);
        if (scroll == null)
            return null;
        return scroll.content;
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

    static Transform FindChild(Transform root, string childName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return child;
            Transform nested = FindChild(child, childName);
            if (nested != null)
                return nested;
        }
        return null;
    }
}
