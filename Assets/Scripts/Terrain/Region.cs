using System.Collections.Generic;
using UnityEngine;
using static MapBuilder;
using static NoiseGenerator;

public class Region {
    public GameObject regionObject;
    public MapData3D mapData;
    public TerrainType[] terrainTypes;
    public Vector3 position;

    public Region(MapData3D mapData, Vector3 inputCoord, Transform parent, Material material) {
        this.mapData = mapData;
        position = inputCoord;

        GenerateRegionObject(parent, material);
        PopulateRegion();
    }

    private void GenerateRegionObject(Transform parent, Material material) {
        //Configure and Instantiate RegionObject
        regionObject = new GameObject("Region(" + position.x + ", " + position.y + ", " + position.z + ")");
        regionObject.isStatic = true;
        regionObject.transform.parent = parent;
        regionObject.transform.position = new Vector3(position.x, position.y, position.z);

        //Layers and Mask
        regionObject.tag = "Terrain";
        regionObject.layer = LayerMask.NameToLayer("Terrain");

        //Instantiate RegionObject Components
        MeshFilter terrainMeshFilter = regionObject.AddComponent<MeshFilter>();
        MeshRenderer terrainMeshRenderer = regionObject.AddComponent<MeshRenderer>();
        MeshCollider terrainMeshCollider = regionObject.AddComponent<MeshCollider>();

        // Generate and Assign Material
        Material terrainMaterial = new Material(material);
        terrainMeshRenderer.sharedMaterial = terrainMaterial;

        // Generate and Assign Terrain Texture
        Texture2D regionTexture = new Texture2D(mapData.dimensions, mapData.dimensions);
        regionTexture.filterMode = FilterMode.Point;
        regionTexture.wrapMode = TextureWrapMode.Clamp;
        regionTexture.SetPixels(mapData.colorMap);
        regionTexture.Apply();
        terrainMeshRenderer.sharedMaterial.mainTexture = regionTexture;

        // Generate and Assign Mesh
        terrainMeshFilter.sharedMesh = terrainMeshCollider.sharedMesh = mapData.meshData.CreateMesh();
    }

    private void PopulateRegion() {
        Vector2[] seedPoints = null;
        NoiseData2D noiseMap = mapData.noiseData;
        Dictionary<string, (int minHeightIndex, int maxHeightIndex)> terrainRangeLimits = new Dictionary<string, (int minHeightIndex, int maxHeightIndex)>();

        for (int x = 0; x < mapData.dimensions; x++) {
            for (int y = 0; y < mapData.dimensions; y++) {
                foreach(TerrainType terrain in terrainTypes) {
                    if (terrain.heightMax > noiseMap.map[x, y] && terrain.heightMin < noiseMap.map[x, y]) {

                    }
                    // if both are filled (both max and min) then calculate median index
                    // Add to seed points
                }

            }
        }

        //Worley(map, seedPoints)
    }

    [System.Serializable]
    public struct TerrainType {
        public string name;
        public float heightMax;
        public float heightMin;
        public Color colour;

        public Resource[] resources;
    }

    [System.Serializable]
    public struct Resource {
        public string name;
        
    }
}

