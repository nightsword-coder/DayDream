using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RolePanel : MonoBehaviour
{
    public BoardGrid board;
    public Image portrait;
    public TMP_Text healthText;
    public TMP_Text physicalAttackText;
    public TMP_Text magicAttackText;
    public TMP_Text manaText;

    CanvasGroup _group;
    float _openAlpha = 150f / 255f;
    Role _shown;
    readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    void Awake()
    {
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
        SetText(healthText, role.health.ToString());
        SetText(physicalAttackText, role.physicalAttack.ToString());
        SetText(magicAttackText, role.magicAttack.ToString());
        SetMana(role);
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

        SetText(healthText, role.health.ToString());
        SetText(physicalAttackText, role.physicalAttack.ToString());
        SetText(magicAttackText, role.magicAttack.ToString());
        SetMana(role);
    }

    void SetMana(Role role)
    {
        if (manaText != null)
            manaText.text = role.currentMana + "/" + role.manaMax;
    }

    void Hide()
    {
        _group.alpha = 0f;
    }

    static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
