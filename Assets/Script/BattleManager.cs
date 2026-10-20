using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public Button endTurnButton;
    public GameObject victoryPanel;
    public GameObject FailPanel;

    readonly List<Role> _order = new List<Role>();
    int _index;
    bool _ready;
    bool _over;

    public Role Current { get; private set; }

    void Awake()
    {
        Instance = this;
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(EndTurn);
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
        if (FailPanel != null)
            FailPanel.SetActive(false);
    }

    void Update()
    {
        if (_over)
            return;

        if (!_ready)
        {
            if (BoardGrid.Instance == null || BoardGrid.Instance.cellContent == null)
                return;
            BuildOrder();
            _ready = true;
            BeginTurn();
        }

        if (ProtagonistDead())
            Lose();
        else if (AllEnemiesDead())
            Win();
    }

    public void EndTurn()
    {
        if (_over || !_ready || _order.Count == 0)
            return;
        if (ProtagonistDead())
        {
            Lose();
            return;
        }
        if (AllEnemiesDead())
        {
            Win();
            return;
        }

        _index++;
        BeginTurn();
    }

    void BuildOrder()
    {
        _order.Clear();
        BoardGrid board = BoardGrid.Instance;
        for (int row = 0; row < board.rows; row++)
        {
            for (int col = 0; col < board.columns; col++)
            {
                Role role = RoleOn(board, col, row);
                if (role != null && !IsEnemy(role))
                    _order.Add(role);
            }
        }

        for (int row = 0; row < board.rows; row++)
        {
            for (int col = 0; col < board.columns; col++)
            {
                Role role = RoleOn(board, col, row);
                if (IsEnemy(role))
                    _order.Add(role);
            }
        }
    }

    void BeginTurn()
    {
        if (_order.Count == 0)
            return;

        int guard = 0;
        while (guard < _order.Count)
        {
            Role role = _order[_index % _order.Count];
            if (role != null && role.health > 0)
            {
                Current = role;
                BoardGrid.Instance.HighlightRole(role);
                Enemy enemy = role.GetComponent<Enemy>();
                if (enemy != null)
                    enemy.Act();
                return;
            }

            _index++;
            guard++;
        }
    }

    bool ProtagonistDead()
    {
        for (int i = 0; i < _order.Count; i++)
        {
            Role role = _order[i];
            if (role != null && !IsEnemy(role) && role.health <= 0)
                return true;
        }
        return false;
    }

    bool AllEnemiesDead()
    {
        bool found = false;
        for (int i = 0; i < _order.Count; i++)
        {
            Role role = _order[i];
            if (!IsEnemy(role))
                continue;
            found = true;
            if (role.health > 0)
                return false;
        }
        return found;
    }

    void Win()
    {
        _over = true;
        Current = null;
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    void Lose()
    {
        _over = true;
        Current = null;
        if (FailPanel != null)
            FailPanel.SetActive(true);
    }

    static bool IsEnemy(Role role)
    {
        return role != null && role.GetComponent<Enemy>() != null;
    }

    static Role RoleOn(BoardGrid board, int col, int row)
    {
        BoardGrid.CellContent content = board.cellContent[col, row];
        if (content == null || content.instance == null)
            return null;
        return content.instance.GetComponent<Role>();
    }
}
