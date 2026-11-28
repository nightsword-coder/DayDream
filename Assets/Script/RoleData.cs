using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一名角色的基础数据。生命、攻击和蓝量的当前值仍记在 Role 上。
/// </summary>
[CreateAssetMenu(fileName = "Role", menuName = "DayDream/角色")]
public class RoleData : ScriptableObject
{
    public string roleName;
    public string EnglishName;
    public Sprite portrait;
    public int health = 30;
    public int physicalAttack = 10;
    public int magicAttack = 10;
    public int manaMax = 10;
    public int manaRegen;
    public int moveRange = 4;
    public int range = 1;
    public Sprite attack;
    public GameObject attackTrail;
    public GameObject[] projectiles;
    public Sprite poisonSprite;
    public Sprite wetSprite;
    public Sprite frozenSprite;
    public List<Skill> skills = new List<Skill>();
}
