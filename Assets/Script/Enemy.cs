using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在敌人棋子上，和 Role 放在一起。轮到自己时沿最短路尽量靠近玩家，攻击一次，然后结束回合。
/// </summary>
[RequireComponent(typeof(Role))]
public class Enemy : MonoBehaviour
{
    Role _role;
    Coroutine _turn;

    void Awake()
    {
        _role = GetComponent<Role>();
    }

    public void Act()
    {
        if (_turn != null)
            StopCoroutine(_turn);
        _turn = StartCoroutine(TakeTurn());
    }

    IEnumerator TakeTurn()
    {
        BoardGrid board = BoardGrid.Instance;
        if (board == null || _role == null || _role.health <= 0)
            yield break;

        Role player = FindPlayer(board);
        if (player == null || !CellOf(board, player, out int playerCol, out int playerRow))
        {
            EndIfStillMyTurn();
            yield break;
        }

        board.HighlightRole(_role);
        if (TryApproach(board, playerCol, playerRow, out int destCol, out int destRow)
            && board.TryMoveTo(destCol, destRow))
        {
            while (board.Moving && StillMyTurn())
                yield return null;
        }

        if (!StillMyTurn())
            yield break;

        if (CellOf(board, _role, out int selfCol, out int selfRow)
            && CellOf(board, player, out playerCol, out playerRow))
            board.FaceToward(selfCol, selfRow, playerCol, playerRow);

        board.HighlightRole(_role);
        bool attacked = CellOf(board, player, out playerCol, out playerRow)
            && board.TryAttack(playerCol, playerRow);
        if (attacked && PoisonWillHurt() && StillMyTurn())
            yield return new WaitForSeconds(0.5f);

        EndIfStillMyTurn();
    }

    bool TryApproach(BoardGrid board, int playerCol, int playerRow, out int destCol, out int destRow)
    {
        destCol = -1;
        destRow = -1;
        if (!CellOf(board, _role, out int selfCol, out int selfRow))
            return false;

        List<Vector2Int> path = board.FindShortestPath(selfCol, selfRow, playerCol, playerRow, true);
        if (path == null || path.Count < 3)
            return false;

        int steps = Mathf.Min(Mathf.Max(0, _role.moveRange), path.Count - 2);
        if (steps <= 0)
            return false;

        destCol = path[steps].x;
        destRow = path[steps].y;
        return true;
    }

    bool PoisonWillHurt()
    {
        return _role != null && _role.poisonTurns > 0 && _role.healthMax * 10 / 100 > 0;
    }

    void EndIfStillMyTurn()
    {
        if (StillMyTurn())
            BattleManager.Instance.EndTurn();
    }

    bool StillMyTurn()
    {
        return BattleManager.Instance != null && BattleManager.Instance.Current == _role;
    }

    static Role FindPlayer(BoardGrid board)
    {
        if (board.cellContent == null)
            return null;

        for (int row = 0; row < board.rows; row++)
        {
            for (int col = 0; col < board.columns; col++)
            {
                Role role = RoleOn(board, col, row);
                if (role != null && role.GetComponent<Enemy>() == null && role.health > 0)
                    return role;
            }
        }

        return null;
    }

    static bool CellOf(BoardGrid board, Role role, out int col, out int row)
    {
        col = -1;
        row = -1;
        if (board == null || role == null || board.cellContent == null)
            return false;

        for (int r = 0; r < board.rows; r++)
        {
            for (int c = 0; c < board.columns; c++)
            {
                if (RoleOn(board, c, r) != role)
                    continue;
                col = c;
                row = r;
                return true;
            }
        }

        return false;
    }

    static Role RoleOn(BoardGrid board, int col, int row)
    {
        BoardGrid.CellContent content = board.cellContent[col, row];
        if (content == null || content.instance == null)
            return null;
        return content.instance.GetComponent<Role>();
    }
}
