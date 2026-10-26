using UnityEngine;
using UnityEngine.UI;

public class BoardActions : MonoBehaviour
{
    public BoardGrid board;
    public Button moveButton;
    public Button attackButton;

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

    void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (moveButton != null)
            moveButton.interactable = battle == null || battle.CanMove;
        if (attackButton != null)
            attackButton.interactable = battle == null || battle.CanAttack;
    }
}
