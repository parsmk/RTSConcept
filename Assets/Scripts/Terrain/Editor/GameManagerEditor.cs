using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor: Editor {
    public override void OnInspectorGUI() {
        GameManager gameManager = (GameManager)target;

        if (GUILayout.Button("Build Map")) {
            gameManager.GenerateMap();
        }

        if (GUILayout.Button("Test")) {
            gameManager.Test();
        }

        DrawDefaultInspector();
    }
}