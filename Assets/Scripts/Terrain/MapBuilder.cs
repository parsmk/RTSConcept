using System.Collections.Generic;
using UnityEngine;
using static MapGenerator;

public class MapBuilder : MonoBehaviour {
    public GameObject mapWrapper;
    public Material meshMaterial;

    public bool autoUpdate = false;

    public int mapRegionDimensions = 2;
    public static int regionSize = MapGenerator.dimensions - 1;

    private Dictionary<Vector2, Region> regions = new Dictionary<Vector2, Region>();

    public void BuildMap() {
        MapGenerator mapGen = GetComponent<MapGenerator>();
        float maxMapHeight = 0;
        float minMapHeight = 0;
        for (int x = 0; x < mapRegionDimensions; x++) {
            for (int y = 0; y < mapRegionDimensions; y++) {
                Vector2 coord = new Vector2(x, y) * regionSize;
                MapData mapData = mapGen.GenerateMap(coord);

                if (!regions.ContainsKey(coord)) {
                    regions.Add(coord, new Region(mapData, coord, mapWrapper.transform, meshMaterial));
                }

                if (maxMapHeight < mapData.noiseData.localMax)
                    maxMapHeight = mapData.noiseData.localMax;
                if (minMapHeight > mapData.noiseData.localMin)
                    minMapHeight = mapData.noiseData.localMin;
            }
        }

        foreach (Vector2 key in regions.Keys) {
            MapData normalizedData = mapGen.NormalizeMap(regions[key].mapData, minMapHeight, maxMapHeight);
            regions[key].UpdateRegion(normalizedData);
        }
    }

    public void ClearMap() {
        if (regions.Count < 1)
            return;

        foreach (Vector2 key in regions.Keys)
            DestroyImmediate(regions[key].terrain);

        regions.Clear();
    }

    private void OnValidate() {
        if (!autoUpdate)
            return;
        BuildMap();
    }

    private class Region {
        public GameObject terrain;
        public MapData mapData;
        public Vector2 position = Vector2.zero;

        public MeshRenderer terrainMeshRenderer;
        public MeshFilter terrainMeshFilter;
        public Material terrainMaterial;

        public Region(MapData mapData, Vector2 inputCoord, Transform parent, Material material) {
            this.mapData = mapData;
            // Position and generate Terrain GameObject
            position = inputCoord;
            terrain = new GameObject("Region(" + position.x + ", " + position.y + ")");
            terrain.isStatic = true;
            terrain.transform.parent = parent;
            terrain.transform.position = new Vector3(position.x, 0, position.y);
            terrainMeshFilter = terrain.AddComponent<MeshFilter>();
            terrainMeshRenderer = terrain.AddComponent<MeshRenderer>();

            // Generate Terrain Texture, Material and Mesh
            terrainMaterial = new Material(material);
            terrainMeshRenderer.sharedMaterial = terrainMaterial;
            terrainMeshRenderer.sharedMaterial.mainTexture = GenerateRegionTexture();
            terrainMeshFilter.sharedMesh = mapData.meshData.CreateMesh();
        }

        public void UpdateRegion(MapData mapData) {
            this.mapData = mapData;
            terrainMeshRenderer.sharedMaterial.mainTexture = GenerateRegionTexture();
            terrainMeshFilter.sharedMesh = mapData.meshData.CreateMesh();
        }

        private Texture2D GenerateRegionTexture() {
            Texture2D output = new Texture2D(mapData.dimensions, mapData.dimensions);
            output.filterMode = FilterMode.Point;
            output.wrapMode = TextureWrapMode.Clamp;
            output.SetPixels(mapData.colorMap);
            output.Apply();

            return output;
        }
    }
}