using System.Collections.Generic;
using UnityEngine;
using static NoiseHandler2D;

public class MapBuilder : MonoBehaviour {
    public enum MapType { VisualizeNoise, HeightMap/*, VoxelMap*/ };

    [Header("Handlers")]
    public NoiseHandler2D noiseHandler2D;
    public MeshHandler2D meshHandler2D;
    public TextureHandler2D textureHandler2D;

    [Header("Map Settings")]
    public MapType mapType = MapType.HeightMap;
    public GameObject mapWrapper;
    public int mapDimensions = 2;
    public int mapDepth = 1;

    public Dictionary<Vector3Int, Region> BuildMap2D() {
        Dictionary<Vector3Int, Region> regions = new Dictionary<Vector3Int, Region>();
        float maxMapHeight = 0;
        float minMapHeight = 0;
        (Vector3Int key, NoiseData2D noise)[] noiseData = new (Vector3Int key, NoiseData2D noise)[mapDimensions * mapDimensions];

        for (int x = 0, index = 0; x < mapDimensions; x++) {
            for (int y = 0; y < mapDimensions; y++, index++) {
                Vector3Int coord = new Vector3Int(x, 0, y) * (noiseHandler2D.noiseDimensions - 1);

                noiseData[index].key = coord;
                noiseData[index].noise = noiseHandler2D.GenerateNoise2D(noiseHandler2D.noiseMode, new Vector2(coord.x, coord.z));

                maxMapHeight = Mathf.Max(maxMapHeight, noiseData[index].noise.max);
                minMapHeight = Mathf.Min(minMapHeight, noiseData[index].noise.min);
            }
        }

        for (int i = 0; i < noiseData.Length; i++) {
            noiseData[i].noise = noiseHandler2D.NormalizeNoise(noiseData[i].noise, minMapHeight, maxMapHeight);
        }

        foreach ((Vector3Int key, NoiseData2D noise) entry in noiseData) {
            RegionType regionType = GameManager.GetRegionType("standard");

            Mesh mesh = meshHandler2D.GenerateMesh(entry.noise.dimensions, meshHandler2D.heightModifier, entry.noise.map);
            Texture2D texture = textureHandler2D.ComputeTexture(Color.white, entry.noise.dimensions, entry.noise.map);

            GameObject region = GenerateRegionObject(entry.key, texture, mesh);
            regions.Add(entry.key, new Region(region, regionType, mesh, texture));
        }

        return regions;
    }

    private GameObject GenerateRegionObject(Vector3 position, Texture texture, Mesh mesh) {
        GameObject region = new GameObject($"Region({position.x}, {position.y}, { position.z })");

        region.isStatic = true;
        region.transform.parent = mapWrapper.transform;
        region.transform.position = new Vector3(position.x, position.y, position.z);

        region.tag = "Terrain";
        region.layer = LayerMask.NameToLayer("Terrain");

        MeshFilter regionMeshFilter = region.AddComponent<MeshFilter>();
        MeshRenderer regionMeshRenderer = region.AddComponent<MeshRenderer>();
        MeshCollider regionMeshCollider = region.AddComponent<MeshCollider>();

        Material regionMaterial = new Material(textureHandler2D.material);
        regionMeshRenderer.sharedMaterial = regionMaterial;

        // Assign Texture and Mesh
        texture.wrapMode = TextureWrapMode.Clamp;
        regionMeshRenderer.sharedMaterial.mainTexture = texture;
        regionMeshFilter.sharedMesh = regionMeshCollider.sharedMesh = mesh;

        return region;
    }
}