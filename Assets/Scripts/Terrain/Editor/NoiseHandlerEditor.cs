using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static NoiseHandler2D;

[CustomEditor(typeof(NoiseHandler2D))]
public class NoiseHandlerEditor : Editor {
    List<SerializedProperty> properties = new List<SerializedProperty>();

    SerializedProperty fractalSettings;
    SerializedProperty seed;
    SerializedProperty scale;
    SerializedProperty persistence;
    SerializedProperty lacunarity;
    SerializedProperty octaves;

    SerializedProperty perlinSettings;
    SerializedProperty tSmoothMode;

    private void OnEnable() {
        SerializedProperty serializedProperties = serializedObject.GetIterator();
        fractalSettings = serializedObject.FindProperty("fractalSettings");
        perlinSettings = serializedObject.FindProperty("perlinSettings");
        List<string> excludedProperties = new List<string> { "fractalSettings", "perlinSettings" };

        seed = fractalSettings.FindPropertyRelative("seed");
        scale = fractalSettings.FindPropertyRelative("scale");
        persistence = fractalSettings.FindPropertyRelative("persistence");
        lacunarity = fractalSettings.FindPropertyRelative("lacunarity");
        octaves = fractalSettings.FindPropertyRelative("octaves");

        tSmoothMode = perlinSettings.FindPropertyRelative("tSmoothMode");

        serializedProperties.Next(true);
        while (serializedProperties.NextVisible(false)) {
            if (excludedProperties.Contains(serializedProperties.name)) { continue; }
            properties.Add(serializedProperties.Copy());
        }
    }

    public override void OnInspectorGUI() {
        NoiseHandler2D noiseHandler = (NoiseHandler2D)target;

        serializedObject.Update();

        foreach (SerializedProperty property in properties) {
            EditorGUILayout.PropertyField(property);
            if (property.name.Equals("noiseMode") && noiseHandler.noiseMode == NoiseMode2D.Perlin) {
                EditorGUILayout.PropertyField(tSmoothMode, new GUIContent("TSmoothMode"), true);
            }
        }

        seed.intValue = EditorGUILayout.IntField("Seed", seed.intValue);
        scale.floatValue = EditorGUILayout.FloatField("Scale", scale.floatValue);
        persistence.floatValue = EditorGUILayout.Slider("Persistence", persistence.floatValue, 0f, 1f);
        lacunarity.floatValue = EditorGUILayout.FloatField("Lacunarity", lacunarity.floatValue);
        octaves.intValue = EditorGUILayout.IntField("Octaves", octaves.intValue);

        serializedObject.ApplyModifiedProperties();
    }
}