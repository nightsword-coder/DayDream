using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在战斗场景的物体上，把 Board 拖进来。
/// 选中技能后高亮范围，并等鼠标点下一个目标格，再按技能挂上的函数执行。
/// </summary>
public class SkillExecutor : MonoBehaviour
{
    public static SkillExecutor Instance { get; private set; }

    public BoardGrid board;

    public Role caster { get; private set; }
    public Role target { get; private set; }
    public Skill skill { get; private set; }

    readonly List<Coroutine> _routines = new List<Coroutine>();
    readonly List<Vector2Int> _area = new List<Vector2Int>();
    readonly List<Vector2Int> _cast = new List<Vector2Int>();
    bool _bound;
    bool _click;
    int _clickCol;
    int _clickRow;

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
        BoardGrid grid = Grid;
        if (!Ready(grid, out Role actor, out Skill selected))
        {
            StopRoutines(grid);
            caster = actor;
            skill = selected;
            return;
        }

        if (!_bound || skill != selected || caster != actor)
        {
            StopRoutines(grid);
            caster = actor;
            skill = selected;
            target = null;
            _bound = true;
        }

        Paint(grid, selected, actor);

        if (!TakeClick(out int col, out int row))
            return;

        Cast(actor, grid, selected, col, row);
    }

    void Cast(Role actor, BoardGrid grid, Skill selected, int col, int row)
    {
        StopEffectRoutines();
        if (!HasTarget(actor, grid, selected, col, row) || !actor.SpendMana(selected.mana))
            return;

        int[] ids = selected.effectIds;
        if (ids == null || ids.Length == 0)
            ids = new[] { 0 };

        for (int i = 0; i < ids.Length; i++)
        {
            SkillFx fx = SkillEffects.FunctionAt(ids[i]);
            if (fx == null)
                continue;
            _routines.Add(StartCoroutine(fx(actor, grid, col, row)));
        }
    }

    static bool HasTarget(Role actor, BoardGrid grid, Skill selected, int col, int row)
    {
        if (actor == null || grid == null || selected == null)
            return false;
        if (!grid.TryCellOf(actor, out int originCol, out int originRow))
            return false;
        if (!selected.InRange(originCol, originRow, col, row))
            return false;

        var cells = new List<Vector2Int>();
        selected.CopyCells(col, row, cells);
        for (int i = 0; i < cells.Count; i++)
        {
            Role other = grid.RoleInCell(cells[i].x, cells[i].y);
            if (other != null && other != actor)
                return true;
        }
        return false;
    }

    void Paint(BoardGrid grid, Skill selected, Role actor)
    {
        _area.Clear();
        _cast.Clear();
        if (!grid.TryCellOf(actor, out int originCol, out int originRow))
        {
            grid.ClearSkillMarks();
            return;
        }

        for (int row = 0; row < grid.rows; row++)
        {
            for (int col = 0; col < grid.columns; col++)
            {
                if (selected.InRange(originCol, originRow, col, row))
                    _area.Add(new Vector2Int(col, row));
            }
        }

        if (Camera.main != null
            && grid.TryScreenToCell(Input.mousePosition, out int hoverCol, out int hoverRow)
            && selected.InRange(originCol, originRow, hoverCol, hoverRow))
        {
            selected.CopyCells(hoverCol, hoverRow, _cast);
            for (int i = _area.Count - 1; i >= 0; i--)
            {
                if (Listed(_cast, _area[i].x, _area[i].y))
                    _area.RemoveAt(i);
            }
        }

        grid.ShowSkillMarks(_area, _cast);
    }

    static bool Listed(List<Vector2Int> cells, int col, int row)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].x == col && cells[i].y == row)
                return true;
        }
        return false;
    }

    public bool HandleClick(int col, int row, bool occupied)
    {
        if (!_bound)
            return false;

        _click = true;
        _clickCol = col;
        _clickRow = row;
        return true;
    }

    bool TakeClick(out int col, out int row)
    {
        col = _clickCol;
        row = _clickRow;
        if (!_click)
            return false;
        _click = false;
        return true;
    }

    public void RememberTarget(Role role)
    {
        target = role;
    }

    bool Ready(BoardGrid grid, out Role actor, out Skill selected)
    {
        actor = grid != null ? grid.SelectedRole : null;
        selected = ActionSkills.Instance != null ? ActionSkills.Instance.Selected : null;
        return grid != null && actor != null && selected != null;
    }

    void StopRoutines(BoardGrid grid)
    {
        StopEffectRoutines();
        _bound = false;
        _click = false;
        if (grid != null)
            grid.ClearSkillMarks();
    }

    void StopEffectRoutines()
    {
        for (int i = 0; i < _routines.Count; i++)
        {
            if (_routines[i] != null)
                StopCoroutine(_routines[i]);
        }

        _routines.Clear();
    }

    BoardGrid Grid
    {
        get { return board != null ? board : BoardGrid.Instance; }
    }
}
