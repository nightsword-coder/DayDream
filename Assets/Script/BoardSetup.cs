using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoardSetup", menuName = "DayDream/棋盘开局")]
public class BoardSetup : ScriptableObject
{
    public int columns = 6;
    public int rows = 6;
    public List<InitialPiece> pieces = new List<InitialPiece>();

    [Serializable]
    public class InitialPiece
    {
        public int col;
        public int row;
        public GameObject prefab;
    }

    public GameObject GetPrefab(int col, int row)
    {
        if (pieces == null)
            return null;

        GameObject prefab = null;
        for (int i = 0; i < pieces.Count; i++)
        {
            InitialPiece piece = pieces[i];
            if (piece != null && piece.prefab != null && piece.col == col && piece.row == row)
                prefab = piece.prefab;
        }
        return prefab;
    }

    public void SetPrefab(int col, int row, GameObject prefab)
    {
        if (pieces == null)
            pieces = new List<InitialPiece>();

        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            InitialPiece piece = pieces[i];
            if (piece != null && piece.col == col && piece.row == row)
                pieces.RemoveAt(i);
        }

        if (prefab == null)
            return;

        pieces.Add(new InitialPiece
        {
            col = col,
            row = row,
            prefab = prefab,
        });
    }

    void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
    }
}
