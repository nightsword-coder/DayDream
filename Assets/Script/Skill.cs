using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一项技能。由书引用，商店和背包看到的是同一份资源。
/// 施法中心最远是 range 格（曼哈顿距离，不含自己）。默认 3 格。
/// 施法格子相对鼠标展开，可以超出 range。十字因此能打到第 4 格。
/// 单体留 (0, 0)。十字五格是 (0,0)、(0,1)、(1,0)、(-1,0)、(0,-1)。不填时按单体处理。
/// </summary>
[CreateAssetMenu(fileName = "Skill", menuName = "DayDream/技能")]
public class Skill : ScriptableObject
{
    public static readonly Vector2Int[] SingleCell =
    {
        new Vector2Int(0, 0),
    };

    public static readonly Vector2Int[] CrossCells =
    {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1),
    };

    public string skillName;

    public Sprite image;

    public int mana;

    public int power;

    [Min(0)]
    [Tooltip("施法中心离角色最远几格，上下左右数。十字等形状可以再向外打到。")]
    public int range = 3;

    public int[] effectIds;

    [Tooltip("相对鼠标的格子。x 向右，y 向上。(0,0) 是鼠标所在格。空着则只施法鼠标那一格。")]
    public Vector2Int[] cells =
    {
        new Vector2Int(0, 0),
    };

    [TextArea]
    public string description;

    public bool InRange(int fromCol, int fromRow, int toCol, int toRow)
    {
        int distance = Mathf.Abs(fromCol - toCol) + Mathf.Abs(fromRow - toRow);
        return distance > 0 && distance <= range;
    }

    public void CopyCells(int anchorCol, int anchorRow, List<Vector2Int> into)
    {
        into.Clear();
        if (cells == null || cells.Length == 0)
        {
            into.Add(new Vector2Int(anchorCol, anchorRow));
            return;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            int col = anchorCol + cells[i].x;
            int row = anchorRow + cells[i].y;
            if (Listed(into, col, row))
                continue;
            into.Add(new Vector2Int(col, row));
        }
    }

    static bool Listed(List<Vector2Int> cells, int col, int row)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].x == col && cells[i].y == row)
                return true;
        }
        return false;
    }
}
