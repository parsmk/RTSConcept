using System.Collections.Generic;
using UnityEngine;
using static MapBuilder;
using static NoiseGenerator;

public class Region {
    public RegionType regionType;
    public GameObject regionObject;
    public MapData3D mapData;
    public TerrainType[] terrainTypes;
    public Vector3 position;

    public Region(RegionType regionType, MapData3D mapData, Vector3 inputCoord, Transform parent, Material material) {
        this.mapData = mapData;
        this.regionType = regionType;
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
        NoiseData2D noiseData = mapData.noiseData;

        //GenerateSeedPoints(noiseData);
        //Worley(seedPoints, noiseData);
    }

    private Vector2[] GenerateSeedPoints(NoiseData2D noiseData) {
        Vector2[] output = null;
        Dictionary<string, List<Vector2>> rawTerrainTypes = new Dictionary<string, List<Vector2>>();

        for (int x = 0; x < noiseData.dimensions; x++) {
            for (int y = 0; y < noiseData.dimensions; y++) {
            }
        }

        return output;
    }

    private void FloodFill(NoiseData2D noiseData2D) {
        Stack<Vector2> stack = new Stack<Vector2>();
        bool[,] visited = new bool[mapData.dimensions, mapData.dimensions];
        float[,] noiseMap = noiseData2D.map;

        int x = Random.Range(noiseData2D.dimensions, noiseData2D.dimensions);
        int y = Random.Range(noiseData2D.dimensions, noiseData2D.dimensions);
        Vector2 seed = new Vector2(x, y);

        stack.Push(seed);
        visited[x, y] = true;

        while(stack.Count > 0) {
            Vector2 seedPoint = stack.Pop();

            
        }
    }
}
