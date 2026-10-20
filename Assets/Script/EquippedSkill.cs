using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class EquippedSkill : MonoBehaviour
{
    public static EquippedSkill Instance { get; private set; }

    public Sprite emptyImage;

    readonly Skill[] _equipped = new Skill[4];
    bool _panelWasOpen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        GameObject panel = GameObject.Find("SkillPanel");
        bool open = panel != null && panel.activeInHierarchy;
        if (open && !_panelWasOpen)
            Refresh();
        _panelWasOpen = open;
    }

    public Skill EquippedAt(int index)
    {
        if (index < 0 || index >= _equipped.Length)
            return null;
        return _equipped[index];
    }

    public bool IsEquipped(Skill skill)
    {
        if (skill == null)
            return false;
        for (int i = 0; i < _equipped.Length; i++)
        {
            if (_equipped[i] == skill)
                return true;
        }
        return false;
    }

    public void BindIcon(Transform slot, Skill skill, bool fromEquipped, int equippedIndex)
    {
        Transform imageObject = FindChild(slot, "skillimage");
        if (imageObject == null)
            return;

        Image image = imageObject.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = skill != null;

        SkillIconDrag drag = imageObject.GetComponent<SkillIconDrag>();
        if (drag == null)
            drag = imageObject.gameObject.AddComponent<SkillIconDrag>();
        drag.skill = skill;
        drag.fromEquipped = fromEquipped;
        drag.equippedIndex = equippedIndex;
    }

    public void Drop(SkillIconDrag drag, PointerEventData eventData)
    {
        if (drag == null || drag.skill == null)
            return;

        int slot = SlotUnder(eventData);
        bool onAll = Inside(AllSkillsRect(), eventData);

        if (drag.fromEquipped)
        {
            if (slot >= 0 && slot != drag.equippedIndex)
                Swap(drag.equippedIndex, slot);
            else if (onAll)
                _equipped[drag.equippedIndex] = null;
        }
        else if (slot >= 0)
        {
            for (int i = 0; i < _equipped.Length; i++)
            {
                if (_equipped[i] == drag.skill)
                    _equipped[i] = null;
            }
            _equipped[slot] = drag.skill;
        }

        Refresh();
    }

    void Refresh()
    {
        Transform root = EquippedRoot();
        if (root != null)
        {
            for (int i = 0; i < _equipped.Length; i++)
            {
                Transform slot = FindChild(root, "skill" + (i + 1));
                if (slot == null)
                    continue;
                Apply(slot, _equipped[i]);
                BindIcon(slot, _equipped[i], true, i);
            }
        }

        if (AllSkill.Instance != null)
            AllSkill.Instance.Refresh();
    }

    void Swap(int from, int to)
    {
        Skill skill = _equipped[from];
        _equipped[from] = _equipped[to];
        _equipped[to] = skill;
    }

    void Apply(Transform slot, Skill skill)
    {
        Transform imageObject = FindChild(slot, "skillimage");
        if (imageObject != null)
        {
            Image image = imageObject.GetComponent<Image>();
            if (image != null)
                image.sprite = skill != null ? skill.image : emptyImage;
        }

        SetNumber(slot, skill != null ? skill.mana.ToString() : "", "manatext", "mana");
        SetNumber(slot, skill != null ? skill.power.ToString() : "", "powertext", "power");
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

    int SlotUnder(PointerEventData eventData)
    {
        Transform root = EquippedRoot();
        if (root == null)
            return -1;

        for (int i = 0; i < _equipped.Length; i++)
        {
            Transform slot = FindChild(root, "skill" + (i + 1));
            if (slot == null)
                continue;
            Transform imageObject = FindChild(slot, "skillimage");
            if (imageObject != null && Inside(imageObject as RectTransform, eventData))
                return i;
        }
        return -1;
    }

    static bool Inside(RectTransform rect, PointerEventData eventData)
    {
        if (rect == null)
            return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera);
    }

    static Transform EquippedRoot()
    {
        GameObject panel = FindInScene("SkillPanel");
        if (panel == null)
            return null;
        return FindChild(panel.transform, "equippedskills");
    }

    static RectTransform AllSkillsRect()
    {
        GameObject panel = FindInScene("SkillPanel");
        if (panel == null)
            return null;
        Transform all = FindChild(panel.transform, "allskills");
        if (all == null)
            return null;

        ScrollRect scroll = all.GetComponentInChildren<ScrollRect>(true);
        if (scroll != null && scroll.viewport != null)
            return scroll.viewport;
        return all as RectTransform;
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

public class SkillIconDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Skill skill;
    public bool fromEquipped;
    public int equippedIndex;

    RectTransform _ghost;
    Canvas _canvas;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (skill == null || skill.image == null)
            return;

        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            _canvas = _canvas.rootCanvas;

        var go = new GameObject("DragSkill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (_canvas != null)
            go.transform.SetParent(_canvas.transform, false);
        go.transform.SetAsLastSibling();

        _ghost = go.GetComponent<RectTransform>();
        RectTransform source = transform as RectTransform;
        if (source != null)
            _ghost.sizeDelta = source.rect.size;

        Image image = go.GetComponent<Image>();
        image.sprite = skill.image;
        image.raycastTarget = false;
        MoveGhost(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveGhost(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_ghost != null)
            Destroy(_ghost.gameObject);
        _ghost = null;
        if (EquippedSkill.Instance != null)
            EquippedSkill.Instance.Drop(this, eventData);
    }

    void MoveGhost(PointerEventData eventData)
    {
        if (_ghost == null || _canvas == null)
            return;

        RectTransform canvasRect = _canvas.transform as RectTransform;
        Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, camera, out Vector2 local))
            _ghost.anchoredPosition = local;
    }
}
