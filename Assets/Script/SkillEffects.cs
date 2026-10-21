using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate IEnumerator SkillFx(Role role, BoardGrid board, int col, int row);

public static class SkillEffects
{
    static readonly SkillFx[] Effects =
    {
        Hit,
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

    static IEnumerator Hit(Role role, BoardGrid board, int col, int row)
    {
        Skill skill = SkillExecutor.Instance != null ? SkillExecutor.Instance.skill : null;
        if (role == null || board == null || skill == null)
            yield break;
        if (!board.TryCellOf(role, out int originCol, out int originRow))
            yield break;
        if (!skill.InRange(originCol, originRow, col, row))
            yield break;

        var cells = new List<Vector2Int>();
        skill.CopyCells(col, row, cells);

        var enemies = new List<Role>();
        for (int i = 0; i < cells.Count; i++)
        {
            Role enemy = board.RoleInCell(cells[i].x, cells[i].y);
            if (enemy == null || enemy == role || Listed(enemies, enemy))
                continue;
            enemies.Add(enemy);
        }

        if (enemies.Count == 0 || !Pay(role, skill))
            yield break;

        for (int i = 0; i < enemies.Count; i++)
            Apply(role, skill, enemies[i]);
        Finish();
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
