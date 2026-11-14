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
        DoubleWet,
        Lightning,
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

    static IEnumerator DoubleWet(Role role, BoardGrid board, int col, int row)
    {
        yield break;
    }

    static IEnumerator Lightning(Role role, BoardGrid board, int col, int row)
    {
        if (!Targets(role, board, col, row, out _, out List<Role> enemies))
            yield break;

        GameObject prefab = SkillExecutor.Instance.lightningPrefab;
        for (int i = 0; i < enemies.Count; i++)
        {
            Role enemy = enemies[i];
            if (enemy == null)
                continue;

            SkillExecutor.Instance.RememberTarget(enemy);
            if (prefab == null || !board.TryCellOf(enemy, out int enemyCol, out int enemyRow))
                continue;

            Vector3 at = board.GetCellCenter(enemyCol, enemyRow);
            at.y += 3f;
            at.z = enemy.transform.position.z;
            GameObject fx = Object.Instantiate(prefab, at, Quaternion.identity);
            Object.Destroy(fx, LifeSeconds(fx));
        }

        Finish();
    }

    static float LifeSeconds(GameObject fx)
    {
        Animator animator = fx.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            float longest = 0f;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].length > longest)
                    longest = clips[i].length;
            }
            if (longest > 0f)
                return longest;
        }

        Animation legacy = fx.GetComponentInChildren<Animation>(true);
        if (legacy != null && legacy.clip != null && legacy.clip.length > 0f)
            return legacy.clip.length;

        Renderer renderer = fx.GetComponentInChildren<Renderer>(true);
        if (renderer != null)
        {
            Material material = renderer.sharedMaterial;
            if (material != null && material.HasProperty("_Fllipbook_Columns") && material.HasProperty("_Fllipbook_Rows"))
            {
                float tiles = material.GetFloat("_Fllipbook_Columns") * material.GetFloat("_Fllipbook_Rows");
                float speed = material.HasProperty("_Fllipbook_speed") ? material.GetFloat("_Fllipbook_speed") : 1f;
                if (tiles > 0f && speed > 0f)
                    return tiles / (3f * speed);
            }
        }

        return 10f * Mathf.Max(Time.deltaTime, 1f / 60f);
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

        return enemies.Count > 0;
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

    static void Apply(Role role, Skill skill, Role enemy)
    {
        int power = skill.power;
        if (enemy.wetTurns > 0 && HasEffect(skill, 3))
            power *= 2;
        int damage = role.magicAttack * power / 100;
        SkillExecutor.Instance.RememberTarget(enemy);
        enemy.TakeSkillDamage(damage);
    }

    static bool HasEffect(Skill skill, int id)
    {
        int[] ids = skill.effectIds;
        if (ids == null)
            return false;
        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == id)
                return true;
        }
        return false;
    }

    static void Finish()
    {
        if (ActionSkills.Instance != null)
            ActionSkills.Instance.ClearSelection();
    }
}
