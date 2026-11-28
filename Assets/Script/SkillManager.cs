using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按角色名字记住已装备技能。没有记录时用 Role 上的预设。
/// 场景里的技能栏只能当作塞拉菲娜的初始装备。
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    const string Seraphina = "塞拉菲娜";

    readonly Dictionary<string, List<Skill>> _equipped = new Dictionary<string, List<Skill>>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (_equipped.ContainsKey(Seraphina))
            return;
        if (ActionSkills.Instance == null || ActionSkills.Instance.skills == null)
            return;
        if (ActionSkills.Instance.skills.Count == 0)
            return;
        _equipped[Seraphina] = Copy(ActionSkills.Instance.skills);
    }

    public bool TryGet(string roleName, out List<Skill> skills)
    {
        skills = null;
        if (string.IsNullOrEmpty(roleName))
            return false;
        return _equipped.TryGetValue(roleName, out skills);
    }

    public void ApplyTurn(Role role)
    {
        if (role == null || role.GetComponent<Enemy>() != null)
            return;

        string name = role.roleName;
        if (string.IsNullOrEmpty(name))
            return;

        if (!_equipped.TryGetValue(name, out List<Skill> skills))
        {
            skills = Copy(role.skills);
            _equipped[name] = skills;
        }

        if (ActionSkills.Instance != null)
            ActionSkills.Instance.SetSkills(skills);
        if (EquippedSkill.Instance != null)
            EquippedSkill.Instance.SetSkills(skills);
    }

    static List<Skill> Copy(IList<Skill> source)
    {
        var list = new List<Skill>();
        if (source == null)
            return list;
        for (int i = 0; i < source.Count && list.Count < 4; i++)
            list.Add(source[i]);
        return list;
    }
}
