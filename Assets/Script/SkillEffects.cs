using System.Collections;
using UnityEngine;

public delegate IEnumerator SkillFx(Role role, BoardGrid board, int col, int row);

public static class SkillEffects
{
    static readonly int Facing = Animator.StringToHash("facing");

    static readonly SkillFx[] Effects =
    {
        Hit,        //0基础攻击
        Poison,     //1中毒
        Wet,        //2潮湿
        DoubleWet,  //3潮湿必定暴击
        Lightning,  //4特效
        FaceAway,   //5使对方背向
        Heal,       //6按威力治疗
        Push,       //7击退10格
        Chain,      //8闪电链
        HealSelf,   //9按百分比治疗自己
        Freeze,     //10施加2层冰冻
        Slash,      //11刀光拖尾
        Projectile, //12抛物线投射物
    };

    public static SkillFx FunctionAt(int id)
    {
        if (id < 0 || id >= Effects.Length)
            return null;
        return Effects[id];
    }

    static IEnumerator Hit(Role role, BoardGrid board, int col, int row)
    {
        Skill skill = CurrentSkill;
        if (!Prepared(board, skill))
            yield break;

        for (int i = 0; i < board.TargetCount; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, false))
                continue;
            if (enemy.frozenTurns > 0)
            {
                SkillExecutor.Instance.RememberTarget(enemy);
                enemy.ApplyFrozen(-1);
                continue;
            }
            Apply(role, board, skill, enemy);
        }
        Finish();
    }

    static IEnumerator Slash(Role role, BoardGrid board, int col, int row)
    {
        if (!Prepared(board, CurrentSkill))
            yield break;

        for (int i = 0; i < board.TargetCount; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, false))
                continue;
            SkillExecutor.Instance.RememberTarget(enemy);
            PlaySlash(enemy, SkillExecutor.Instance.slashTrailPrefab, SkillExecutor.Instance);
        }
        Finish();
    }

    const float SlashSeconds = 0.12f;

    public static void PlaySlash(Role target, GameObject template, MonoBehaviour host)
    {
        if (target == null || template == null || host == null)
            return;

        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
        if (sprite == null)
            sprite = target.GetComponentInChildren<SpriteRenderer>(true);
        Vector3 start;
        Vector3 end;
        if (sprite != null && sprite.sprite != null)
        {
            Bounds bounds = sprite.bounds;
            float z = bounds.center.z - 0.2f;
            start = new Vector3(bounds.min.x, bounds.max.y, z);
            end = new Vector3(bounds.max.x, bounds.min.y, z);
        }
        else
        {
            Vector3 at = target.transform.position;
            start = at + new Vector3(-0.4f, 0.5f, -0.2f);
            end = at + new Vector3(0.4f, -0.5f, -0.2f);
        }

        GameObject slash = Object.Instantiate(template, target.transform);
        slash.name = "Slash";
        Vector3 scale = target.transform.lossyScale;
        slash.transform.localScale = new Vector3(
            Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x,
            Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y,
            Mathf.Approximately(scale.z, 0f) ? 1f : 1f / scale.z);
        slash.transform.localRotation = Quaternion.identity;

        TrailRenderer trail = slash.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
            if (sprite != null)
            {
                trail.sortingLayerID = sprite.sortingLayerID;
                trail.sortingOrder = sprite.sortingOrder;
            }
        }

        slash.transform.position = start;
        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }
        host.StartCoroutine(MoveSlash(slash, start, end));
    }

    static IEnumerator MoveSlash(GameObject slash, Vector3 start, Vector3 end)
    {
        float time = 0f;
        while (time < SlashSeconds)
        {
            if (slash == null)
                yield break;
            time += Time.deltaTime;
            slash.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(time / SlashSeconds));
            yield return null;
        }

        if (slash != null)
            Object.Destroy(slash);
    }

    const float ProjectileLift = 0.45f;
    const float ProjectileSpeed = 10f;

    static IEnumerator Projectile(Role role, BoardGrid board, int col, int row)
    {
        if (!Prepared(board, CurrentSkill))
            yield break;

        GameObject prefab = FirstProjectile(role);
        int count = board.TargetCount;
        var shots = new GameObject[count];
        var starts = new Vector3[count];
        var ends = new Vector3[count];
        var heights = new float[count];
        var durations = new float[count];
        float longest = 0f;

        for (int i = 0; i < count; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, false))
                continue;

            SkillExecutor.Instance.RememberTarget(enemy);
            if (prefab == null)
                continue;

            Vector3 start = role.transform.position;
            start.y += ProjectileLift;
            Vector3 end = enemy.transform.position;
            float span = Vector2.Distance(start, end);
            float height = Mathf.Clamp(Mathf.Max(span, 1.5f) * 0.85f, 2.4f, 4.5f);
            float duration = Mathf.Clamp((span + height) / ProjectileSpeed, 0.5f, 0.7f);

            GameObject shot = Object.Instantiate(prefab, start, Quaternion.identity);
            shots[i] = shot;
            starts[i] = start;
            ends[i] = end;
            heights[i] = height;
            durations[i] = duration;
            if (duration > longest)
                longest = duration;
            AimProjectile(shot.transform, ProjectileTangent(start, end, height, 0f));
            LiftSorting(shot, enemy);
        }

        if (longest <= 0f)
        {
            Finish();
            yield break;
        }

        float time = 0f;
        while (time < longest)
        {
            time += Time.deltaTime;
            for (int i = 0; i < count; i++)
            {
                GameObject shot = shots[i];
                if (shot == null)
                    continue;

                float u = Mathf.Clamp01(time / durations[i]);
                shot.transform.position = ProjectilePoint(starts[i], ends[i], heights[i], u);
                AimProjectile(shot.transform, ProjectileTangent(starts[i], ends[i], heights[i], u));
            }

            if (time >= longest)
                break;
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (shots[i] != null)
                Object.Destroy(shots[i]);
        }
        Finish();
    }

    static GameObject FirstProjectile(Role role)
    {
        GameObject[] list = role != null ? role.projectiles : null;
        if (list == null)
            return null;

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
                return list[i];
        }
        return null;
    }

    static Vector3 ProjectilePoint(Vector3 start, Vector3 end, float height, float u)
    {
        Vector3 rise = start;
        rise.y += height;
        Vector3 dive = end;
        dive.y += height;
        float t = Mathf.Clamp01(u);
        float stay = 1f - t;
        return stay * stay * stay * start
            + 3f * stay * stay * t * rise
            + 3f * stay * t * t * dive
            + t * t * t * end;
    }

    static Vector3 ProjectileTangent(Vector3 start, Vector3 end, float height, float u)
    {
        Vector3 rise = start;
        rise.y += height;
        Vector3 dive = end;
        dive.y += height;
        float t = Mathf.Clamp01(u);
        float stay = 1f - t;
        return 3f * stay * stay * (rise - start)
            + 6f * stay * t * (dive - rise)
            + 3f * t * t * (end - dive);
    }

    static void AimProjectile(Transform shot, Vector3 tangent)
    {
        float dx = tangent.x;
        float dy = tangent.y;
        if (dx * dx + dy * dy < 0.0001f)
            return;

        // 预制件默认竖直向上，0 度是正上方，落到终点时转到正下方。
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - 90f;
        shot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    static void LiftSorting(GameObject shot, Role target)
    {
        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
        if (sprite == null)
            sprite = target.GetComponentInChildren<SpriteRenderer>(true);
        if (sprite == null)
            return;

        Renderer[] renderers = shot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerID = sprite.sortingLayerID;
            renderers[i].sortingOrder = sprite.sortingOrder + 2;
        }
    }

    static IEnumerator Freeze(Role role, BoardGrid board, int col, int row)
    {
        if (!Prepared(board, CurrentSkill))
            yield break;

        for (int i = 0; i < board.TargetCount; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, false))
                continue;
            SkillExecutor.Instance.RememberTarget(enemy);
            enemy.ApplyFrozen(2);
        }
        Finish();
    }

    static IEnumerator Poison(Role role, BoardGrid board, int col, int row)
    {
        if (!Prepared(board, CurrentSkill))
            yield break;

        for (int i = 0; i < board.TargetCount; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, false))
                continue;
            SkillExecutor.Instance.RememberTarget(enemy);
            enemy.ApplyPoison(2);
        }
        Finish();
    }

    static IEnumerator Wet(Role role, BoardGrid board, int col, int row)
    {
        if (!Prepared(board, CurrentSkill))
            yield break;

        for (int i = 0; i < board.TargetCount; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, false))
                continue;
            SkillExecutor.Instance.RememberTarget(enemy);
            enemy.ApplyWet(2);
        }
        Finish();
    }

    static IEnumerator DoubleWet(Role role, BoardGrid board, int col, int row)
    {
        yield break;
    }

    static IEnumerator Lightning(Role role, BoardGrid board, int col, int row)
    {
        if (!Prepared(board, CurrentSkill))
            yield break;

        GameObject prefab = SkillExecutor.Instance.lightningPrefab;
        for (int i = 0; i < board.TargetCount; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, false))
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

    static IEnumerator FaceAway(Role role, BoardGrid board, int col, int row)
    {
        if (!Prepared(board, CurrentSkill))
            yield break;
        if (!board.TryCellOf(role, out int casterCol, out int casterRow))
            yield break;

        for (int i = 0; i < board.TargetCount; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, false) || !board.TryCellOf(enemy, out int enemyCol, out int enemyRow))
                continue;

            int dx = enemyCol - casterCol;
            int dy = enemyRow - casterRow;
            if (dx == 0 && dy == 0)
                continue;

            SkillExecutor.Instance.RememberTarget(enemy);
            board.FaceToward(enemyCol, enemyRow, enemyCol + dx, enemyRow + dy);
        }
        Finish();
    }

    static IEnumerator Heal(Role role, BoardGrid board, int col, int row)
    {
        Skill skill = CurrentSkill;
        if (!Prepared(board, skill))
            yield break;

        int amount = role.magicAttack * skill.power / 100;
        for (int i = 0; i < board.TargetCount; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, true))
                continue;
            SkillExecutor.Instance.RememberTarget(enemy);
            Restore(enemy, amount);
        }
        Finish();
    }

    static IEnumerator Push(Role role, BoardGrid board, int col, int row)
    {
        if (!Prepared(board, CurrentSkill))
            yield break;
        if (!board.TryCellOf(role, out int casterCol, out int casterRow))
            yield break;

        int count = board.TargetCount;
        var stepCol = new int[count];
        var stepRow = new int[count];
        var active = new bool[count];
        for (int i = 0; i < count; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, false) || !board.TryCellOf(enemy, out int enemyCol, out int enemyRow))
                continue;
            if (!AwayStep(casterCol, casterRow, enemyCol, enemyRow, out int pushCol, out int pushRow))
                continue;

            SkillExecutor.Instance.RememberTarget(enemy);
            active[i] = true;
            stepCol[i] = pushCol;
            stepRow[i] = pushRow;
        }
        Finish();

        var stopped = new bool[count];
        //推动10格
        for (int step = 0; step < 10; step++)
        {
            yield return new WaitForSeconds(0.1f);
            bool any = false;
            var moved = new bool[count];
            for (int n = 0; n < count; n++)
            {
                int pick = Farthest(board, casterCol, casterRow, active, stopped, moved);
                if (pick < 0)
                    break;

                moved[pick] = true;
                if (!PushOne(board, board.TargetAt(pick), stepCol[pick], stepRow[pick]))
                {
                    stopped[pick] = true;
                    continue;
                }
                any = true;
            }
            if (!any)
                yield break;
        }
    }

    static IEnumerator Chain(Role role, BoardGrid board, int col, int row)
    {
        Skill skill = CurrentSkill;
        if (!Prepared(board, skill))
            yield break;

        for (int i = 0; i < board.TargetCount; i++)
        {
            Role enemy = board.TargetAt(i);
            if (!Affects(role, enemy, false))
                continue;
            Role next = NextEnemy(role, board, enemy);
            if (next == null)
                continue;
            Apply(role, board, skill, next);
        }
        Finish();
    }

    static IEnumerator HealSelf(Role role, BoardGrid board, int col, int row)
    {
        if (!Prepared(board, CurrentSkill))
            yield break;

        Restore(role, (role.healthMax + 3) / 4);
        Finish();
    }

    static void Restore(Role target, int amount)
    {
        if (target == null || amount <= 0 || target.health >= target.healthMax)
            return;

        int before = target.health;
        target.health += amount;
        if (target.health > target.healthMax)
            target.health = target.healthMax;
        int gained = target.health - before;
        if (gained <= 0)
            return;

        DamagePopup.SpawnHeal(target, gained);
        if (General.Instance != null)
            General.Instance.Refresh(target);
    }

    static bool AwayStep(int casterCol, int casterRow, int targetCol, int targetRow, out int stepCol, out int stepRow)
    {
        stepCol = 0;
        stepRow = 0;
        int dx = targetCol - casterCol;
        int dy = targetRow - casterRow;
        if (dx == 0 && dy == 0)
            return false;

        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            stepCol = dx > 0 ? 1 : -1;
        else
            stepRow = dy > 0 ? 1 : -1;
        return true;
    }

    static int Farthest(BoardGrid board, int casterCol, int casterRow, bool[] active, bool[] stopped, bool[] moved)
    {
        int pick = -1;
        int best = -1;
        for (int i = 0; i < active.Length; i++)
        {
            if (!active[i] || stopped[i] || moved[i])
                continue;
            Role target = board.TargetAt(i);
            if (target == null || !board.TryCellOf(target, out int col, out int row))
            {
                stopped[i] = true;
                continue;
            }

            int distance = Mathf.Abs(col - casterCol) + Mathf.Abs(row - casterRow);
            if (distance <= best)
                continue;
            best = distance;
            pick = i;
        }
        return pick;
    }

    const int CollisionDamage = 5;

    static bool PushOne(BoardGrid board, Role target, int stepCol, int stepRow)
    {
        if (target == null || !board.TryCellOf(target, out int col, out int row))
            return false;

        int nextCol = col + stepCol;
        int nextRow = row + stepRow;
        if (nextCol < 0 || nextRow < 0 || nextCol >= board.columns || nextRow >= board.rows)
        {
            target.TakeSkillDamage(CollisionDamage);
            return false;
        }

        if (board.cellContent[nextCol, nextRow] != null)
        {
            Role other = board.RoleInCell(nextCol, nextRow);
            target.TakeSkillDamage(CollisionDamage);
            if (other != null && other != target)
                other.TakeSkillDamage(CollisionDamage);
            return false;
        }
        return board.TryShiftPiece(col, row, nextCol, nextRow);
    }

    static Role NextEnemy(Role caster, BoardGrid board, Role source)
    {
        if (source == null || !board.TryCellOf(source, out int originCol, out int originRow))
            return null;

        Role found = null;
        int best = int.MaxValue;
        for (int row = 0; row < board.rows; row++)
        {
            for (int col = 0; col < board.columns; col++)
            {
                int distance = Mathf.Abs(col - originCol) + Mathf.Abs(row - originRow);
                if (distance <= 0 || distance > 2 || distance >= best)
                    continue;

                Role other = board.RoleInCell(col, row);
                if (!IsFoe(caster, source, other))
                    continue;

                best = distance;
                found = other;
            }
        }
        return found;
    }

    static bool IsFoe(Role caster, Role source, Role other)
    {
        if (other == null || other == caster || other == source || other.health <= 0)
            return false;

        bool casterIsEnemy = caster.GetComponent<Enemy>() != null;
        bool otherIsEnemy = other.GetComponent<Enemy>() != null;
        return casterIsEnemy != otherIsEnemy;
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

    static Skill CurrentSkill
    {
        get { return SkillExecutor.Instance != null ? SkillExecutor.Instance.skill : null; }
    }

    static bool Prepared(BoardGrid board, Skill skill)
    {
        return board != null && skill != null && board.TargetCount > 0;
    }

    static bool Affects(Role caster, Role target, bool anyone)
    {
        if (target == null)
            return false;
        if (anyone || target != caster)
            return true;
        Skill skill = CurrentSkill;
        return skill != null && skill.targetSelf;
    }

    static void Apply(Role role, BoardGrid board, Skill skill, Role enemy)
    {
        int power = skill.power;
        if (enemy.wetTurns > 0 && HasEffect(skill, 3))
            power *= 2;
        int damage = role.magicAttack * power / 100;
        //物理伤害高就用物理
        if(role.physicalAttack*power/100 > damage){
            damage = role.physicalAttack * power / 100;
        }
        if (Behind(role, board, enemy))
            damage = damage * 125 / 100;
        SkillExecutor.Instance.RememberTarget(enemy);
        enemy.TakeSkillDamage(damage);
    }

    public static bool Behind(Role role, BoardGrid board, Role target)
    {
        if (board == null || role == null || target == null)
            return false;
        if (!board.TryCellOf(role, out int roleCol, out int roleRow))
            return false;
        if (!board.TryCellOf(target, out int targetCol, out int targetRow))
            return false;

        int roleFacing = ReadFacing(role);
        int targetFacing = ReadFacing(target);
        return DirectlyBehind(roleCol, roleRow, roleFacing, targetCol, targetRow, targetFacing);
    }

    static bool DirectlyBehind(int roleCol, int roleRow, int roleFacing, int targetCol, int targetRow, int targetFacing)
    {
        int col = roleCol - targetCol;
        int row = roleRow - targetRow;
        if (col == 0 && row == 0)
            return false;

        if (targetFacing == 1)
            return row == 0 && col < 0;
        if (targetFacing == 2)
            return row == 0 && col > 0;
        if (targetFacing == 3)
            return col == 0 && row < 0;
        if (targetFacing == 4)
            return col == 0 && row > 0;
        return false;
    }

    static int ReadFacing(Role role)
    {
        Animator animator = role.GetComponentInChildren<Animator>(true);
        if (animator == null)
            return 0;

        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter parameter = animator.GetParameter(i);
            if (parameter.nameHash == Facing && parameter.type == AnimatorControllerParameterType.Int)
                return animator.GetInteger(Facing);
        }
        return 0;
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
