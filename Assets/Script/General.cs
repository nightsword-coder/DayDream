using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class General : MonoBehaviour
{
    public static General Instance { get; private set; }

    public BoardGrid board;
    public Image portrait;
    public TMP_Text healthText;
    public TMP_Text nameText;
    public TMP_Text physicalAttackText;
    public TMP_Text magicAttackText;
    public TMP_Text manaText;

    Role _shown;
    bool _opened;
    readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!_opened)
            TryShowProtagonist();

        if (Input.GetMouseButtonDown(0))
            HandleClick();
    }

    void TryShowProtagonist()
    {
        BoardGrid grid = board != null ? board : BoardGrid.Instance;
        if (grid == null || grid.cellContent == null)
            return;

        _opened = true;
        Role role = Protagonist(grid);
        if (role == null)
            return;

        _shown = role;
        Write(role);
    }

    static Role Protagonist(BoardGrid grid)
    {
        for (int row = 0; row < grid.rows; row++)
        {
            for (int col = 0; col < grid.columns; col++)
            {
                BoardGrid.CellContent content = grid.cellContent[col, row];
                if (content == null || content.instance == null)
                    continue;
                Role role = content.instance.GetComponent<Role>();
                if (role != null && role.GetComponent<Enemy>() == null)
                    return role;
            }
        }
        return null;
    }

    public void Refresh(Role role)
    {
        if (role == null || role != _shown)
            return;
        Write(role);
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
            return;

        _shown = role;
        Write(role);
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

    void Write(Role role)
    {
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

        SetText(healthText, role.health + "/" + role.healthMax);
        SetText(nameText, role.roleName);
        SetText(physicalAttackText, role.physicalAttack.ToString());
        SetText(magicAttackText, role.magicAttack.ToString());
        if (manaText != null)
            manaText.text = role.currentMana + "/" + role.manaMax;
    }

    static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
