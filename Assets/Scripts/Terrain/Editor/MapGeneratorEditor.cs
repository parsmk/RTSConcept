using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {

    public override void OnInspectorGUI() {
        MapGenerator terrainGen = (MapGenerator)target;

        DrawDefaultInspector();
    }
}