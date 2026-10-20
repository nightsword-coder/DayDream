using UnityEngine;

/// <summary>
/// 一项技能。由书引用，商店和背包看到的是同一份资源。
/// </summary>
[CreateAssetMenu(fileName = "Skill", menuName = "DayDream/技能")]
public class Skill : ScriptableObject
{
    public string skillName;

    public Sprite image;

    public int mana;

    public int power;

    public int[] effectIds;

    [TextArea]
    public string description;
}
