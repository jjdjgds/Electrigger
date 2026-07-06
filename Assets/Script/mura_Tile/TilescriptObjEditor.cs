#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TilescriptObj))]
public class TilescriptObjEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var data = (TilescriptObj)target;
        int expectedSize = TilescriptObj.width * TilescriptObj.height;
        if (data.tiles == null || data.tiles.Length != expectedSize)
        {
            data.tiles = new bool[expectedSize];
            EditorUtility.SetDirty(data);
        }

        for (int y = 0; y < TilescriptObj.height; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < TilescriptObj.width; x++)
            {
                int index = y * TilescriptObj.width + x;
                data.tiles[index] = EditorGUILayout.Toggle(
                    data.tiles[index],
                    GUILayout.Width(20)
                );
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUI.changed) EditorUtility.SetDirty(data);
    }
}
#endif