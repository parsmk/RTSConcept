using System.Collections.Generic;
using UnityEngine;
using static NoiseGenerator;
using static MeshGenerator;
using static ColorGenerator;

public class MapBuilder : MonoBehaviour {
    public GameObject mapWrapper;

    public enum MapType { HeightMap, ComplexMap };
    public MapType mapType = MapType.HeightMap;

    public int mapDimensions = 2;
    //[HideInInspector]
    public int mapDepth = 1;
    public static int regionSize = noiseDimensions - 1;

    #region NoiseSettings
    [Header("Noise Settings")]
    public NoiseMode noiseMode;
    public NoiseInterpolateMode noiseInterpolateMode;
    public NoiseLocalInterpolateMode noiseLocalInterpolateMode;

    public int seed = 1;
    public float scale = 20;
    public const int noiseDimensions = 241;
    public Vector3 mapOffset = Vector3.zero;

    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int octaves = 4;
    #endregion

    #region TextureSettings
    [Header("Texture Settings")]
    public DrawMode drawMode;
    //[HideInInspector]
    public Color color;
    //[HideInInspector]
    public TerrainFeature[] terrainTypes;
    public float textureDetail = 2;
    #endregion

    #region MeshSettings
    [Header("Mesh Settings")]
    public int meshDetail = 5;
    [Range(0, 1)]
    public float threshhold = 0.5f;
    public int heightModifier = 50;

    //[HideInInspector]
    public AnimationCurve animationCurve;
    public Material meshMaterial;
    #endregion

    private Dictionary<Vector3, Region> regions = new Dictionary<Vector3, Region>();

    public void BuildMap() {
        switch(mapType) {
            case MapType.HeightMap:
                BuildMap2D();
                break;
            case MapType.ComplexMap:
                BuildMap3D();
                break;
        }
    }

    private void BuildMap2D() {
        float maxMapHeight = 0;
        float minMapHeight = 0;
        (Vector3 key, NoiseData2D noise)[] noiseData = new (Vector3 key, NoiseData2D noise)[mapDimensions * mapDimensions];

        for (int x = 0, index = 0; x < mapDimensions; x++) {
            for (int y = 0; y < mapDimensions; y++, index++) {
                Vector3 coord = new Vector3(x, 0, y) * regionSize;

                noiseData[index].key = coord;
                noiseData[index].noise = GenerateNoise(
                                seed,
                                scale,
                                noiseDimensions,
                                new Vector2(coord.x + mapOffset.x, coord.z + mapOffset.y),
                                lacunarity,
                                persistence,
                                octaves,
                                noiseMode,
                                noiseInterpolateMode,
                                noiseLocalInterpolateMode
                );


                if (maxMapHeight < noiseData[index].noise.localMax)
                    maxMapHeight = noiseData[index].noise.localMax;
                if (minMapHeight > noiseData[index].noise.localMin)
                    minMapHeight = noiseData[index].noise.localMin;
            }
        }

        for (int x = 0, index = 0; x < mapDimensions; x++) {
            for (int y = 0; y < mapDimensions; y++, index++) {
                noiseData[index].noise = NormalizeNoise(noiseData[index].noise, minMapHeight, maxMapHeight);
            }
        }

        foreach ((Vector3 key, NoiseData2D noise) entry in noiseData) {
            MeshData meshData = GenerateMeshData(entry.noise.map, noiseDimensions, heightModifier, animationCurve);
            Color[] colorMap = ChooseColorMap(entry.noise.map);
            MapData mapData = new MapData(noiseDimensions, entry.noise, meshData, colorMap);

            regions.Add(entry.key, new Region(mapData, entry.key, mapWrapper.transform, meshMaterial));
        }
    }

    private void BuildMap3D() {
        float maxMapHeight = 0;
        float minMapHeight = 0;
        (Vector3 key, NoiseData3D noise)[] noiseData = new (Vector3 key, NoiseData3D noise)[mapDimensions * mapDimensions];

        for (int x = 0, index = 0; x < mapDimensions; x++) {
            for (int y = 0; y < mapDimensions; y++) {
                for (int z = 0; z < mapDepth; z++, index++) {
                    Vector3 coord = new Vector3(x, z, y) * regionSize;

                    noiseData[index].key = coord;
                    noiseData[index].noise = GenerateNoise(
                                    seed,
                                    scale,
                                    noiseDimensions,
                                    coord + mapOffset,
                                    lacunarity,
                                    persistence,
                                    octaves,
                                    noiseMode,
                                    noiseInterpolateMode,
                                    noiseLocalInterpolateMode
                    );


                    if (maxMapHeight < noiseData[index].noise.localMax)
                        maxMapHeight = noiseData[index].noise.localMax;
                    if (minMapHeight > noiseData[index].noise.localMin)
                        minMapHeight = noiseData[index].noise.localMin;
                }
            }
        }

        for (int x = 0, index = 0; x < mapDimensions; x++) {
            for (int y = 0; y < mapDimensions; y++) {
                for (int z = 0; z < mapDepth; z++, index++) {
                    noiseData[index].noise = NormalizeNoise(noiseData[index].noise, minMapHeight, maxMapHeight);
                }
            }
        }

        foreach ((Vector3 key, NoiseData3D noise) entry in noiseData) {
            MeshData meshData = GenerateMeshData(entry.noise.map, noiseDimensions, threshhold, textureDetail, meshDetail);
            //TODO:: Work on colormap
            Color[] colorMap = ChooseColorMap(entry.noise.map);
            MapData mapData = new MapData(noiseDimensions, entry.noise, meshData, colorMap);

            regions.Add(entry.key, new Region(mapData, entry.key, mapWrapper.transform, meshMaterial));
        }    
    }

    private Color[] ChooseColorMap(float[,] map) {
        Color[] output = null;

        switch(drawMode) {
            case DrawMode.ColorMap:
                output = GenerateForColorMap(noiseDimensions, map, color);
                break;
            case DrawMode.TerrainMap:
                output = GenerateForTerrainMap(noiseDimensions, map, terrainTypes);
                break;
        }

        return output;
    }

    private Color[] ChooseColorMap(float[,,] map) {
        Color[] output = new Color[noiseDimensions * noiseDimensions * noiseDimensions];

        //switch (drawMode) {
        //    case DrawMode.ColorMap:
        //        output = GenerateForColorMap(noiseDimensions, map, color);
        //        break;
        //    case DrawMode.TerrainMap:
        //        output = GenerateForTerrainMap(noiseDimensions, map, terrainTypes);
        //        break;
        //}

        for (int x = 0; x < noiseDimensions; x++) {
            for (int y = 0; y < noiseDimensions; y++) {
                for (int z = 0; z < noiseDimensions; z++) {
                    output[x + (y * noiseDimensions) + z * (noiseDimensions * noiseDimensions)] = Color.red;
                }
            }
        }

        return output;
    }

    public void ClearMap() {
        if (regions.Count < 1)
            return;

        foreach (Vector2 key in regions.Keys)
            DestroyImmediate(regions[key].regionObject);

        regions.Clear();
    }

    private void OnValidate() {
        // NoiseSettings
        if (lacunarity < 1) { lacunarity = 1; }
        if (octaves < 1) { octaves = 1; }

        // MeshSettings
        if (meshDetail < 5) { meshDetail = 5; }
        while ((noiseDimensions - 1) % meshDetail != 0) { meshDetail++; }
        if (heightModifier < 0) { heightModifier = 0; }
    }

    private class Region {
        public GameObject regionObject;
        public MapData mapData;
        public TerrainFeature[] regionFeatures;
        public Vector3 position = Vector3.zero;

        public MeshRenderer terrainMeshRenderer;
        public MeshFilter terrainMeshFilter;
        public Material terrainMaterial;

        public Region(MapData mapData, Vector3 inputCoord, Transform parent, Material material) {
            this.mapData = mapData;

            // Position and generate Terrain GameObject
            position = inputCoord;
            regionObject = new GameObject("Region(" + position.x + ", " + position.y + ", " + position.z + ")");
            regionObject.isStatic = true;
            regionObject.transform.parent = parent;
            regionObject.transform.position = new Vector3(position.x, position.y, position.z);
            terrainMeshFilter = regionObject.AddComponent<MeshFilter>();
            terrainMeshRenderer = regionObject.AddComponent<MeshRenderer>();

            // Generate Terrain Texture, Material and Mesh
            terrainMaterial = new Material(material);
            terrainMeshRenderer.sharedMaterial = terrainMaterial;
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

    public struct MapData {
        public int dimensions;
        public NoiseData3D noiseData;
        public MeshData meshData;
        public Color[] colorMap;

        public MapData(int dimensions, NoiseData3D noiseData, MeshData meshData, Color[] colorMap) {
            this.dimensions = dimensions;
            this.noiseData = noiseData;
            this.meshData = meshData;
            this.colorMap = colorMap;
        }
    }

    [System.Serializable]
    public struct TerrainFeature {
        public string name;
        public float heightThreshold;
        public Color colour;
    }
}