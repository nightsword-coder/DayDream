using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Skill))]
public class SkillEditor : Editor
{
    const int Half = 3;
    static bool _listOpen;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("skillName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("image"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mana"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("power"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("range"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("effectIds"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("施法格子", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("中间是鼠标所在格。点一格就加上或去掉。x 向右，y 向上。不填任何格子时，只施法鼠标那一格。", MessageType.None);

        SerializedProperty cells = serializedObject.FindProperty("cells");
        DrawPresets(cells);
        DrawGrid(cells);

        EditorGUILayout.Space();
        _listOpen = EditorGUILayout.Foldout(_listOpen, "格子列表");
        if (_listOpen)
            EditorGUILayout.PropertyField(cells, GUIContent.none, true);

        serializedObject.ApplyModifiedProperties();
    }

    static void DrawPresets(SerializedProperty cells)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("单体"))
            Assign(cells, Skill.SingleCell);
        if (GUILayout.Button("十字"))
            Assign(cells, Skill.CrossCells);
        EditorGUILayout.EndHorizontal();
    }

    static void DrawGrid(SerializedProperty cells)
    {
        for (int y = Half; y >= -Half; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int x = -Half; x <= Half; x++)
            {
                bool on = Contains(cells, x, y);
                bool center = x == 0 && y == 0;
                Color previous = GUI.backgroundColor;
                if (center)
                    GUI.backgroundColor = on ? new Color(1f, 0.55f, 0.2f) : new Color(0.75f, 0.75f, 0.75f);
                else if (on)
                    GUI.backgroundColor = new Color(1f, 0.35f, 0.3f);

                string label = center ? "中" : (on ? "●" : "");
                if (GUILayout.Button(label, GUILayout.Width(28), GUILayout.Height(28)))
                    Toggle(cells, x, y);
                GUI.backgroundColor = previous;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    static void Assign(SerializedProperty cells, Vector2Int[] preset)
    {
        cells.arraySize = preset.Length;
        for (int i = 0; i < preset.Length; i++)
            cells.GetArrayElementAtIndex(i).vector2IntValue = preset[i];
    }

    static void Toggle(SerializedProperty cells, int x, int y)
    {
        if (cells.arraySize == 0)
        {
            cells.arraySize = 1;
            cells.GetArrayElementAtIndex(0).vector2IntValue = Vector2Int.zero;
            if (x == 0 && y == 0)
                return;
        }

        int found = Find(cells, x, y);
        if (found >= 0)
        {
            if (cells.arraySize == 1)
                return;
            cells.DeleteArrayElementAtIndex(found);
            return;
        }

        int index = cells.arraySize;
        cells.arraySize = index + 1;
        cells.GetArrayElementAtIndex(index).vector2IntValue = new Vector2Int(x, y);
    }

    static bool Contains(SerializedProperty cells, int x, int y)
    {
        if (cells.arraySize == 0)
            return x == 0 && y == 0;
        return Find(cells, x, y) >= 0;
    }

    static int Find(SerializedProperty cells, int x, int y)
    {
        for (int i = 0; i < cells.arraySize; i++)
        {
            Vector2Int value = cells.GetArrayElementAtIndex(i).vector2IntValue;
            if (value.x == x && value.y == y)
                return i;
        }
        return -1;
    }
}
