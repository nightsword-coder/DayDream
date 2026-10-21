using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate IEnumerator SkillFx(Role role, BoardGrid board, int col, int row);

public static class SkillEffects
{
    static readonly SkillFx[] Effects =
    {
        Hit,
        Poison,
        Wet,
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

    static IEnumerator Hit(Role role, BoardGrid board, int col, int row)
    {
        if (!Targets(role, board, col, row, out Skill skill, out List<Role> enemies))
            yield break;

        for (int i = 0; i < enemies.Count; i++)
            Apply(role, skill, enemies[i]);
        Finish();
    }

    static IEnumerator Poison(Role role, BoardGrid board, int col, int row)
    {
        if (!Targets(role, board, col, row, out _, out List<Role> enemies))
            yield break;

        for (int i = 0; i < enemies.Count; i++)
        {
            SkillExecutor.Instance.RememberTarget(enemies[i]);
            enemies[i].ApplyPoison(2);
        }
        Finish();
    }

    static IEnumerator Wet(Role role, BoardGrid board, int col, int row)
    {
        if (!Targets(role, board, col, row, out _, out List<Role> enemies))
            yield break;

        for (int i = 0; i < enemies.Count; i++)
        {
            SkillExecutor.Instance.RememberTarget(enemies[i]);
            enemies[i].ApplyWet(2);
        }
        Finish();
    }

    static bool Targets(Role role, BoardGrid board, int col, int row, out Skill skill, out List<Role> enemies)
    {
        skill = SkillExecutor.Instance != null ? SkillExecutor.Instance.skill : null;
        enemies = null;
        if (role == null || board == null || skill == null)
            return false;
        if (!board.TryCellOf(role, out int originCol, out int originRow))
            return false;
        if (!skill.InRange(originCol, originRow, col, row))
            return false;

        var cells = new List<Vector2Int>();
        skill.CopyCells(col, row, cells);
        enemies = new List<Role>();
        for (int i = 0; i < cells.Count; i++)
        {
            Role enemy = board.RoleInCell(cells[i].x, cells[i].y);
            if (enemy == null || enemy == role || Listed(enemies, enemy))
                continue;
            enemies.Add(enemy);
        }

        return enemies.Count > 0 && Pay(role, skill);
    }

    static bool Listed(List<Role> enemies, Role enemy)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == enemy)
                return true;
        }
        return false;
    }

    static bool Pay(Role role, Skill skill)
    {
        return skill != null && role.SpendMana(skill.mana);
    }

    static void Apply(Role role, Skill skill, Role enemy)
    {
        int damage = role.magicAttack * skill.power / 100;
        SkillExecutor.Instance.RememberTarget(enemy);
        enemy.TakeSkillDamage(damage);
    }

    static void Finish()
    {
        if (ActionSkills.Instance != null)
            ActionSkills.Instance.ClearSelection();
    }
}
