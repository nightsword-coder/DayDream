using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate IEnumerator SkillFx(Role role, BoardGrid board);

/// <summary>
/// 写死的函数表。技能用编号指向这里，0 号是单体施法。
/// </summary>
public static class SkillEffects
{
    static readonly SkillFx[] Effects =
    {
        CastSkill,
        null,
        null,
        null,
        null,
        null,
    };

    public static SkillFx FunctionAt(int id)
    {
        if (id < 0 || id >= Effects.Length)
            return null;
        return Effects[id];
    }

    static IEnumerator CastSkill(Role role, BoardGrid board)
    {
        int range = role.range + 1;
        bool aoe = false;

        while (SkillExecutor.Instance != null
            && SkillExecutor.Instance.caster == role
            && SkillExecutor.Instance.skill != null)
        {
            yield return Highlight(role, board, range, aoe);

            if (SkillExecutor.Instance == null || !SkillExecutor.Instance.TakeClick(out int col, out int row))
                continue;

            if (aoe)
                HitArea(role, board, col, row, range);
            else
                HitOne(role, board, col, row, range);
        }

        if (board != null)
            board.ClearSkillMarks();
    }

    static IEnumerator Highlight(Role role, BoardGrid board, int range, bool aoe)
    {
        var cells = new List<Vector2Int>();
        if (CellOf(board, role, out int originCol, out int originRow))
        {
            if (!aoe)
            {
                Collect(board, originCol, originRow, range, false, cells);
            }
            else if (board.TryScreenToCell(Input.mousePosition, out int hoverCol, out int hoverRow)
                && Manhattan(originCol, originRow, hoverCol, hoverRow) <= range)
            {
                Collect(board, hoverCol, hoverRow, 1, true, cells);
            }
        }

        board.ShowSkillMarks(cells);
        yield return null;
    }

    static void HitOne(Role role, BoardGrid board, int col, int row, int range)
    {
        if (!CellOf(board, role, out int originCol, out int originRow))
            return;

        int distance = Manhattan(originCol, originRow, col, row);
        if (distance <= 0 || distance > range)
            return;

        Role enemy = board.RoleInCell(col, row);
        if (enemy == null || enemy == role || !Pay(role))
            return;

        Apply(role, enemy);
        Finish();
    }

    static void HitArea(Role role, BoardGrid board, int col, int row, int range)
    {
        if (!CellOf(board, role, out int originCol, out int originRow))
            return;
        if (Manhattan(originCol, originRow, col, row) > range)
            return;

        var enemies = new List<Role>();
        for (int r = 0; r < board.rows; r++)
        {
            for (int c = 0; c < board.columns; c++)
            {
                if (Manhattan(col, row, c, r) > 1)
                    continue;
                Role enemy = board.RoleInCell(c, r);
                if (enemy == null || enemy == role)
                    continue;
                enemies.Add(enemy);
            }
        }

        if (enemies.Count == 0 || !Pay(role))
            return;

        for (int i = 0; i < enemies.Count; i++)
            Apply(role, enemies[i]);
        Finish();
    }

    static bool Pay(Role role)
    {
        Skill skill = SkillExecutor.Instance != null ? SkillExecutor.Instance.skill : null;
        return skill != null && role.SpendMana(skill.mana);
    }

    static void Apply(Role role, Role enemy)
    {
        Skill skill = SkillExecutor.Instance.skill;
        int damage = role.magicAttack * skill.power / 100;
        SkillExecutor.Instance.RememberTarget(enemy);
        enemy.TakeSkillDamage(damage);
    }

    static void Finish()
    {
        if (ActionSkills.Instance != null)
            ActionSkills.Instance.ClearSelection();
    }

    static void Collect(BoardGrid board, int originCol, int originRow, int range, bool includeOrigin, List<Vector2Int> cells)
    {
        for (int row = 0; row < board.rows; row++)
        {
            for (int col = 0; col < board.columns; col++)
            {
                int distance = Manhattan(originCol, originRow, col, row);
                if (distance > range)
                    continue;
                if (!includeOrigin && distance == 0)
                    continue;
                cells.Add(new Vector2Int(col, row));
            }
        }
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
                if (board.RoleInCell(c, r) != role)
                    continue;
                col = c;
                row = r;
                return true;
            }
        }

        return false;
    }

    static int Manhattan(int fromCol, int fromRow, int toCol, int toRow)
    {
        return Mathf.Abs(fromCol - toCol) + Mathf.Abs(fromRow - toRow);
    }
}
