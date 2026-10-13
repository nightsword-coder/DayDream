using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoardSetup))]
public class BoardSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var setup = (BoardSetup)target;
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("columns"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rows"));
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        //EditorGUILayout.HelpBox("把预制体拖进格子。最下面一行是第 0 行，从左往右是第 0 列。留空表示该格没有棋子。", MessageType.Info);

        for (int row = setup.rows - 1; row >= 0; row--)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(row.ToString(), GUILayout.Width(18));
            for (int col = 0; col < setup.columns; col++)
            {
                GameObject current = setup.GetPrefab(col, row);
                EditorGUI.BeginChangeCheck();
                var next = (GameObject)EditorGUILayout.ObjectField(
                    current,
                    typeof(GameObject),
                    false,
                    GUILayout.Width(54),
                    GUILayout.Height(54));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(setup, "设置开局棋子");
                    setup.SetPrefab(col, row, next);
                    EditorUtility.SetDirty(setup);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(22);
        for (int col = 0; col < setup.columns; col++)
            EditorGUILayout.LabelField(col.ToString(), GUILayout.Width(54));
        EditorGUILayout.EndHorizontal();
    }
}
