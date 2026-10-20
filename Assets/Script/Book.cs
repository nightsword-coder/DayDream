using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一本书的数据。商店出售和背包查看都引用同一份资源。
/// </summary>
[CreateAssetMenu(fileName = "Book", menuName = "DayDream/书")]
public class Book : ScriptableObject
{
    public string bookName;

    public Sprite sprite;

    [TextArea]
    public string description;

    public List<Skill> skills = new List<Skill>();
}
