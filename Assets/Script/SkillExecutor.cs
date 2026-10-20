using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在战斗场景的物体上，把 Board 拖进来。
/// 选中技能后，按技能挂上的函数执行。函数参数是施法者和棋盘。
/// </summary>
public class SkillExecutor : MonoBehaviour
{
    public static SkillExecutor Instance { get; private set; }

    public BoardGrid board;

    public Role caster { get; private set; }
    public Role target { get; private set; }
    public Skill skill { get; private set; }

    readonly List<Coroutine> _routines = new List<Coroutine>();
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

        if (_bound && skill == selected && caster == actor)
            return;

        StopRoutines(grid);
        caster = actor;
        skill = selected;
        target = null;
        _bound = true;

        int[] ids = selected.effectIds;
        if (ids == null)
            return;

        for (int i = 0; i < ids.Length; i++)
        {
            SkillFx fx = SkillEffects.FunctionAt(ids[i]);
            if (fx == null)
                continue;
            _routines.Add(StartCoroutine(fx(actor, grid)));
        }
    }

    public bool HandleClick(int col, int row, bool occupied)
    {
        if (_routines.Count == 0)
            return false;

        _click = true;
        _clickCol = col;
        _clickRow = row;
        return true;
    }

    public bool TakeClick(out int col, out int row)
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
        for (int i = 0; i < _routines.Count; i++)
        {
            if (_routines[i] != null)
                StopCoroutine(_routines[i]);
        }

        _routines.Clear();
        _bound = false;
        _click = false;
        if (grid != null)
            grid.ClearSkillMarks();
    }

    BoardGrid Grid
    {
        get { return board != null ? board : BoardGrid.Instance; }
    }
}
