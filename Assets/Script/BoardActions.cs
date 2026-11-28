using UnityEngine;
using UnityEngine.UI;

public class BoardActions : MonoBehaviour
{
    public BoardGrid board;
    public Button moveButton;
    public Button attackButton;

    static Button _move;
    static Button _attack;
    static readonly Color Spent = new Color(0.4f, 0.4f, 0.4f, 1f);

    void Start()
    {
        Listen(LiveBoard());
    }

    // board2.5 关掉之后，按钮引用还在这个组件上，但 Start 不会跑。
    // 当前棋盘起来时把移动、普攻接到自己身上，高亮才会出现在 board0 下。
    public static void BindTo(BoardGrid active)
    {
        if (active == null || !active.isActiveAndEnabled)
            return;

        BoardActions[] all = FindObjectsByType<BoardActions>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null)
                all[i].Listen(active);
        }

        BindLooseButtons(active);
    }

    // board2.5 删掉后，上面的 BoardActions 一起没了。按钮按名字仍接到当前棋盘。
    static void BindLooseButtons(BoardGrid active)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;
            if (string.Equals(button.name, "Move", System.StringComparison.OrdinalIgnoreCase))
            {
                _move = button;
                button.onClick.RemoveListener(active.BeginMove);
                button.onClick.AddListener(active.BeginMove);
            }
            else if (string.Equals(button.name, "attackbutton", System.StringComparison.OrdinalIgnoreCase))
            {
                _attack = button;
                button.onClick.RemoveListener(active.BeginAttack);
                button.onClick.AddListener(active.BeginAttack);
            }
        }
    }

    void Listen(BoardGrid active)
    {
        if (active == null)
            active = LiveBoard();
        if (active == null)
            return;

        board = active;
        if (moveButton != null)
        {
            _move = moveButton;
            moveButton.onClick.RemoveListener(active.BeginMove);
            moveButton.onClick.AddListener(active.BeginMove);
        }
        if (attackButton != null)
        {
            _attack = attackButton;
            attackButton.onClick.RemoveListener(active.BeginAttack);
            attackButton.onClick.AddListener(active.BeginAttack);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        Apply(_move, enabled);
        Apply(_attack, enabled);
    }

    public static void SetMoveEnabled(bool enabled)
    {
        Apply(_move, enabled);
    }

    public static void SetAttackEnabled(bool enabled)
    {
        Apply(_attack, enabled);
    }

    public static void SetAttackSprite(Sprite sprite)
    {
        if (_attack == null || sprite == null)
            return;

        Image image = _attack.GetComponent<Image>();
        if (image != null)
            image.sprite = sprite;
    }

    static void Apply(Button button, bool available)
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;
        colors.disabledColor = Spent;
        button.colors = colors;
        button.interactable = available;
    }

    BoardGrid LiveBoard()
    {
        if (board != null && board.isActiveAndEnabled)
            return board;
        return BoardGrid.Instance;
    }
}
