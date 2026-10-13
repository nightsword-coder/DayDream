using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RolePanel : MonoBehaviour
{
    public BoardGrid board;
    public Image portrait;
    public Text healthText;
    public Text physicalAttackText;
    public Text magicAttackText;

    CanvasGroup _group;
    float _openAlpha = 150f / 255f;
    Role _shown;

    TMP_Text _healthTmp;
    TMP_Text _physicalTmp;
    TMP_Text _magicTmp;
    readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    void Awake()
    {
        BindChildren();

        _group = GetComponent<CanvasGroup>();
        if (_group == null)
            _group = gameObject.AddComponent<CanvasGroup>();

        Image background = GetComponent<Image>();
        if (background != null && background.color.a > 0.001f)
            _openAlpha = background.color.a;

        if (background != null)
        {
            Color color = background.color;
            color.a = 1f;
            background.color = color;
        }

        _group.interactable = false;
        _group.blocksRaycasts = false;
        Hide();
    }

    void Start()
    {
        if (board == null)
            board = FindObjectOfType<BoardGrid>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleClick();

        if (_shown != null)
            RefreshNumbers(_shown);
    }

    void HandleClick()
    {
        if (PointerOverButton() || board == null || board.cellContent == null || Camera.main == null)
            return;
        if (!board.TryScreenToCell(Input.mousePosition, out int col, out int row))
            return;

        BoardGrid.CellContent content = board.cellContent[col, row];
        Role role = content != null && content.instance != null
            ? content.instance.GetComponent<Role>()
            : null;
        if (role == null)
        {
            if (!board.HasHighlightedPiece)
            {
                _shown = null;
                Hide();
            }
            return;
        }

        _shown = role;
        Show(role);
    }

    bool PointerOverButton()
    {
        if (EventSystem.current == null)
            return false;

        _uiHits.Clear();
        var pointer = new PointerEventData(EventSystem.current);
        pointer.position = Input.mousePosition;
        EventSystem.current.RaycastAll(pointer, _uiHits);
        for (int i = 0; i < _uiHits.Count; i++)
        {
            if (_uiHits[i].gameObject.GetComponentInParent<Button>() != null)
                return true;
        }
        return false;
    }

    void RefreshNumbers(Role role)
    {
        SetText(healthText, _healthTmp, role.health.ToString());
        SetText(physicalAttackText, _physicalTmp, role.physicalAttack.ToString());
        SetText(magicAttackText, _magicTmp, role.magicAttack.ToString());
    }

    void Show(Role role)
    {
        _group.alpha = _openAlpha;

        if (portrait != null)
        {
            Sprite sprite = role.portrait;
            if (sprite == null)
            {
                SpriteRenderer piece = role.GetComponent<SpriteRenderer>();
                if (piece != null)
                    sprite = piece.sprite;
            }
            portrait.sprite = sprite;
            portrait.enabled = sprite != null;
        }

        SetText(healthText, _healthTmp, role.health.ToString());
        SetText(physicalAttackText, _physicalTmp, role.physicalAttack.ToString());
        SetText(magicAttackText, _magicTmp, role.magicAttack.ToString());
    }

    void Hide()
    {
        _group.alpha = 0f;
    }

    void BindChildren()
    {
        if (portrait == null)
        {
            Transform found = transform.Find("portrait");
            if (found != null)
                portrait = found.GetComponent<Image>();
        }

        if (healthText != null && physicalAttackText != null && magicAttackText != null)
            return;

        Text[] texts = GetComponentsInChildren<Text>(true);
        if (texts.Length >= 3)
        {
            if (healthText == null)
                healthText = texts[0];
            if (physicalAttackText == null)
                physicalAttackText = texts[1];
            if (magicAttackText == null)
                magicAttackText = texts[2];
            return;
        }

        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        if (tmps.Length >= 3)
        {
            _healthTmp = tmps[0];
            _physicalTmp = tmps[1];
            _magicTmp = tmps[2];
        }
    }

    static void SetText(Text text, TMP_Text tmp, string value)
    {
        if (text != null)
            text.text = value;
        else if (tmp != null)
            tmp.text = value;
    }
}
