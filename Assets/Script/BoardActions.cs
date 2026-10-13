using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在场景物体上，在检查器里指定棋盘和按钮。
/// skill 这一阶段没有功能。
/// </summary>
public class BoardActions : MonoBehaviour
{
    public BoardGrid board;
    public Button moveButton;
    public Button attackButton;
    public Button skillButton;

    void Start()
    {
        if (board == null)
            board = FindObjectOfType<BoardGrid>();
        if (board == null)
            return;

        if (moveButton != null)
            moveButton.onClick.AddListener(board.BeginMove);
        if (attackButton != null)
            attackButton.onClick.AddListener(board.BeginAttack);
    }
}
