using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionSkills : MonoBehaviour
{
    public static ActionSkills Instance { get; private set; }

    public List<Skill> skills = new List<Skill>();
    public Sprite emptyImage;

    static readonly Color Highlight = new Color(0.62f, 0.28f, 0.85f, 1f);
    static readonly Color Unusable = new Color(120f / 255f, 120f / 255f, 120f / 255f, 1f);

    EquippedSkill _source;
    BoardGrid _board;
    int _selected = -1;
    int _shownMana = int.MinValue;
    int _shownCaster;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        _source = EquippedSkill.Instance;
        _board = FindObjectOfType<BoardGrid>();
        Show();
    }

    void Update()
    {
        Role caster = Caster();
        int mana = caster != null ? caster.currentMana : -1;
        int casterId = caster != null ? caster.GetInstanceID() : 0;
        if (mana != _shownMana || casterId != _shownCaster)
        {
            _shownMana = mana;
            _shownCaster = casterId;
            if (_selected >= 0 && !CanAfford(SkillAt(_selected)))
                _selected = -1;
            Show();
        }

        if (!Input.GetMouseButtonDown(0))
            return;

        int index = SlotUnderMouse();
        if (index < 0 || !CanAfford(SkillAt(index)))
            return;
        _selected = _selected == index ? -1 : index;
        if (_selected >= 0 && BoardGrid.Instance != null)
            BoardGrid.Instance.EndCommand();
        Show();
    }

    public Skill Selected
    {
        get { return _selected < 0 ? null : SkillAt(_selected); }
    }

    public void ClearSelection()
    {
        _selected = -1;
        Show();
    }

    void Show()
    {
        Transform root = transform.Find("equippedskills");
        if (root == null)
            return;

        if (_selected >= 0 && SkillAt(_selected) == null)
            _selected = -1;

        for (int i = 0; i < 4; i++)
        {
            Transform slot = FindChild(root, "skill" + (i + 1));
            if (slot == null)
                continue;

            Skill skill = SkillAt(i);
            Transform imageObject = FindChild(slot, "skillimage");
            if (imageObject != null)
            {
                Image image = imageObject.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = skill != null ? skill.image : emptyImage;
                    if (skill != null && !CanAfford(skill))
                        image.color = Unusable;
                    else
                        image.color = i == _selected ? Highlight : Color.white;
                }
            }

            SetNumber(slot, skill != null ? skill.mana.ToString() : "", "manatext", "mana");
            SetNumber(slot, skill != null ? skill.power.ToString() : "", "powertext", "power");
        }
    }

    int SlotUnderMouse()
    {
        Transform root = transform.Find("equippedskills");
        if (root == null)
            return -1;

        Camera camera = UiCamera();
        for (int i = 0; i < 4; i++)
        {
            if (SkillAt(i) == null)
                continue;
            Transform slot = FindChild(root, "skill" + (i + 1));
            if (slot == null)
                continue;
            Transform imageObject = FindChild(slot, "skillimage");
            if (imageObject == null)
                continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(imageObject as RectTransform, Input.mousePosition, camera))
                return i;
        }
        return -1;
    }

    bool CanAfford(Skill skill)
    {
        if (skill == null)
            return false;
        Role caster = Caster();
        if (caster == null)
            return true;
        return caster.currentMana >= skill.mana;
    }

    Role Caster()
    {
        if (_board == null)
            _board = FindObjectOfType<BoardGrid>();
        if (_board == null)
            return null;
        return _board.SelectedRole;
    }

    Skill SkillAt(int index)
    {
        if (index >= 0 && index < skills.Count && skills[index] != null)
            return skills[index];
        if (_source == null)
            return null;
        return _source.EquippedAt(index);
    }

    Camera UiCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return canvas.worldCamera;
        return null;
    }

    static void SetNumber(Transform slot, string value, params string[] childNames)
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
            text.text = value;
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
