using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [Header("Map Properties")]
    public MapBuilder mapBuilder;

    private static Dictionary<string, ScriptableObject> scriptableObjects = new();
    private Dictionary<Vector3Int, Region> regions = new();

    public void GenerateMap() {
        regions = mapBuilder.BuildMap2D();
    }

    public void ClearMap() {
        if (regions.Count < 1)
            return;

        foreach (Region region in regions.Values)
            DestroyImmediate(region.regionObject);

        regions.Clear();
    }

    public void Test() {
        //noiseHandler.GenerateNoise(NoiseHandler2D.NoiseMode2D.Perlin);
        return;
    }

    public static RegionType GetRegionType(string regionName) {
        if (scriptableObjects.ContainsKey(regionName))
            return (RegionType)scriptableObjects[regionName];

        XDocument source = XDocument.Parse(Resources.Load<TextAsset>("Data/region_types").text);

        TerrainType[] terrainTypes = TerrainType.LoadTerrainTypes(regionName, source);
        RegionType regionType = RegionType.LoadRegionType(regionName, source, terrainTypes);

        foreach(TerrainType terrainType in terrainTypes) { 
            if (!scriptableObjects.ContainsKey(terrainType.name))
                scriptableObjects.Add(terrainType.terrainTypeName, terrainType); 
        }
        scriptableObjects.Add(regionName, regionType);

        return (RegionType)scriptableObjects[regionName];
    }

}