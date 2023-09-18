using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(MapBuilder))]
public class MapBuilderEditor: Editor {

    public override void OnInspectorGUI() {
        MapBuilder mapBuilder = (MapBuilder)target;

        DrawDefaultInspector();

        if (mapBuilder.autoUpdate == true) {
            mapBuilder.BuildMap();
        }

        if (GUILayout.Button("Build Map")) {
            mapBuilder.ClearMap();
            mapBuilder.BuildMap();
        }
    }
}