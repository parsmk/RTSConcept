using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapBuilder))]
public class MapBuilderEditor: Editor {
    public override void OnInspectorGUI() {
        MapBuilder mapBuilder = (MapBuilder)target;

        if (GUILayout.Button("Build Map")) {
            mapBuilder.ClearMap();
            mapBuilder.BuildMap();
        }

        DrawDefaultInspector();

        //if (mapBuilder.mapType == )

        //if (mapBuilder.drawMode == MapBuilder.DrawMode.TerrainMap) {
        //        SerializeField
        //}
    }
}