using System;
using UnityEditor;
using UnityEngine;

public sealed class DungeonToolNewMapWindow : EditorWindow
{
    private const string WindowTitle = "Create New Dungeon";

    private int _width;
    private int _height;
    private Action<int, int> _onCreate;

    public static void Open(int width, int height, Action<int, int> onCreate)
    {
        DungeonToolNewMapWindow window = CreateInstance<DungeonToolNewMapWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window._width = Mathf.Max(3, width);
        window._height = Mathf.Max(3, height);
        window._onCreate = onCreate;
        window.minSize = new Vector2(260f, 110f);
        window.maxSize = new Vector2(260f, 140f);
        window.ShowUtility();
    }

    private void OnGUI()
    {
        GUILayout.Label(WindowTitle, EditorStyles.boldLabel);
        _width = Mathf.Max(3, EditorGUILayout.IntField("Width", _width));
        _height = Mathf.Max(3, EditorGUILayout.IntField("Height", _height));

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create"))
            {
                _onCreate?.Invoke(_width, _height);
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
        }
    }
}
