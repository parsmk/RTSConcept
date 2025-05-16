using System.Collections.Generic;
using UnityEngine;
using static NoiseHandler2D;
using static ResourceHandler2D;

public class MapBuilder : MonoBehaviour {

    [Header("2D Handlers")]
    public NoiseHandler2D noiseHandler2D;
    public MeshHandler2D meshHandler2D;
    public TextureHandler2D textureHandler2D;
    public ResourceHandler2D resourceHandler2D;

    [Header("Map Settings")]
    public GameObject mapWrapper;
    public int mapDimensions = 2;
    public int mapDepth = 1;

    public Dictionary<Vector3Int, Region> BuildMap2D() {
        Dictionary<Vector3Int, Region> regions = new();
        float maxMapHeight = 0;
        float minMapHeight = 0;
        (Vector3Int position, NoiseData2D noise)[] noiseData = new (Vector3Int position, NoiseData2D noise)[mapDimensions * mapDimensions];

        // GenerateNoise for each Region
        for (int x = 0, index = 0; x < mapDimensions; x++) {
            for (int y = 0; y < mapDimensions; y++, index++) {
                Vector3Int coord = new Vector3Int(x, 0, y) * (noiseHandler2D.noiseDimensions - 1);

                noiseData[index].position = coord;
                noiseData[index].noise = noiseHandler2D.GenerateNoise(noiseHandler2D.noiseMode, new Vector2(coord.x, coord.z));

                maxMapHeight = Mathf.Max(maxMapHeight, noiseData[index].noise.max);
                minMapHeight = Mathf.Min(minMapHeight, noiseData[index].noise.min);
            }
        }

        // Normalize Region noise
        for (int i = 0; i < noiseData.Length; i++) {
            noiseData[i].noise = noiseHandler2D.NormalizeNoise(noiseData[i].noise, minMapHeight, maxMapHeight);
        }

        // Generate Mesh, Texture, GameObject and Resources
        resourceHandler2D._landMasses = new();
        foreach ((Vector3Int position, NoiseData2D noise) in noiseData) {
            RegionType regionType = GameManager.GetRegionType("standard");

            Mesh mesh = meshHandler2D.GenerateMesh(noise.dimensions, meshHandler2D.heightModifier, noise.map);
            Texture2D texture = textureHandler2D.GenerateTexture(regionType.terrainTypes, noise.dimensions, noise.map);

            GameObject region = GenerateRegionObject(position, texture, mesh);
            GenerateRegionResources(noise, position, regionType.terrainTypes);

            regions.Add(position, new Region(region, regionType, mesh, texture));
        }

        return regions;
    }
    
    private void GenerateRegionResources(NoiseData2D noise, Vector3 position, TerrainType[] terrainTypes) {
        List<LandMass2D> landMasses = resourceHandler2D.GenerateLandMasses(noise.dimensions, new Vector2(position.x, position.z), noise.map, terrainTypes);
        List<Vector2> seedPoints = new();

        foreach(LandMass2D landMass in landMasses) {
            seedPoints.Add(landMass.seedPoint);
        }

        //NoiseData2D noiseData = noiseHandler2D.GenerateNoise(NoiseMode2D.Worley, seedPoints.ToArray());
    }

    private GameObject GenerateRegionObject(Vector3 position, Texture texture, Mesh mesh) {
        GameObject region = new($"Region({position.x}, {position.y}, {position.z})");
        region.isStatic = true;
        region.transform.parent = mapWrapper.transform;
        region.transform.position = new Vector3(position.x, position.y, position.z);

        region.tag = "Terrain";
        region.layer = LayerMask.NameToLayer("Terrain");

        MeshFilter regionMeshFilter = region.AddComponent<MeshFilter>();
        MeshRenderer regionMeshRenderer = region.AddComponent<MeshRenderer>();
        MeshCollider regionMeshCollider = region.AddComponent<MeshCollider>();

        Material regionMaterial = new(textureHandler2D.material);
        regionMeshRenderer.sharedMaterial = regionMaterial;

        // Assign Texture and Mesh
        regionMeshRenderer.sharedMaterial.mainTexture = texture;
        regionMeshFilter.sharedMesh = regionMeshCollider.sharedMesh = mesh;

        return region;
    }
}