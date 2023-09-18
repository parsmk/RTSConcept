using UnityEngine;
using static NoiseGenerator;

public class MapGenerator : MonoBehaviour {
    public enum MapType { HeightMap, ComplexMap };
    public enum DrawMode { ColorMap, TerrainMap };

    #region NoiseSettings
    [Header("Noise")]
    public MapType mapType = MapType.HeightMap;
    public NoiseInterpolateMode noiseInterpolateMode;
    public NoiseLocalInterpolateMode noiseLocalInterpolateMode;

    public int seed = 1;
    public float scale = 20;
    public const int dimensions = 241;
    public Vector3 mapOffset = Vector3.zero;

    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int octaves = 4;
    #endregion

    #region MeshSettings
    [Header("Mesh")]
    //3D
    [HideInInspector]
    public int levelOfDetail = 5;
    [Range(0, 1)]
    public float threshhold = 0.5f;

    //2D
    [HideInInspector]
    public int heightModifier = 50;
    [HideInInspector]
    public AnimationCurve meshHeightCurve;
    #endregion

    #region TextureSettings
    [Header("Texture")]
    public DrawMode drawMode;
    [HideInInspector]
    public Color color;
    [HideInInspector]
    public TerrainType[] terrainTypes;
    #endregion

    //TODO Fix NoiseData3D>2D casting issue
    //TODO MeshData for 3D
        //Finish Marching Cubes algorithm
    //TODO ColorMaps for 2D

    //TODO Figure out how to make the 2D 3D thing cleaner.

    public MapData GenerateMap(Vector3 ? inputOffset = null, NoiseData3D ? noiseData = null) {

        Vector3 regionOffset = mapOffset + (inputOffset ?? Vector3.zero);

        //switch(mapType) {
        //    case MapType.HeightMap:
        //        noiseData ??= GenerateNoise2D(
        //                                seed,
        //                                scale,
        //                                dimensions,
        //                                regionOffset,
        //                                lacunarity,
        //                                persistence,
        //                                octaves,
        //                                noiseInterpolateMode,
        //                                noiseLocalInterpolateMode
        //                            );
        //        meshData = GenerateMeshData(dimensions, noiseData.Value.map, heightModifier, meshHeightCurve);
        //}
        MeshData meshData = MarchCubes(dimensions, noiseData.Value.map);
        Color[] colorMap = GenerateColorMap(noiseData.Value.map);

        return new MapData(dimensions, noiseData.Value, colorMap, meshData);
    }

    public MapData NormalizeMap(MapData mapData, float globalMin, float globalMax) {
        return GenerateMap(null, NormalizeNoise3D(mapData.noiseData, globalMin, globalMax));
    }

    #region ColorMapGeneration
    private Color[] GenerateColorMap(float[,,] map) {
        Color[] colorMap;
        switch (drawMode) {
            case DrawMode.TerrainMap:
                colorMap = GenerateForTerrainMap(dimensions, map, terrainTypes);
                break;
            default:
                colorMap = GenerateForColorMap(dimensions, map, color);
                break;
        }
        return colorMap;
    }

    private static Color[] GenerateForColorMap(int dimensions, float[,,] map, Color color) {
        Color[] colorMap = new Color[dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int z = 0; z < dimensions; z++) {
                for (int x = 0; x < dimensions; x++) {
                    colorMap[(y + z * dimensions) * dimensions + x] = new Color(map[x, y, z] * color.r, map[x, y, z] * color.g, map[x, y, z] * color.b);
                }
            }
        }

        return colorMap;
    }

    private static Color[] GenerateForTerrainMap(int dimensions, float[,,] map, TerrainType[] terrainTypes) {
        Color[] colorMap = new Color[dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                for (int z = 0; z < dimensions; z++) {
                    foreach (TerrainType terrainType in terrainTypes) {
                        if (map[x, y, z] <= terrainType.heightThreshold) {
                            colorMap[y * dimensions + x] = terrainType.colour;
                            break;
                        }
                    }
                }
            }
        }

        return colorMap;
    }
    #endregion

    #region MeshGeneration
    

    public MeshData MarchCubes(int dimensions, float[,,] map) {

        int edgeSize = dimensions / levelOfDetail;
        float[] cubeVertices = new float[8];
        int cubeIndex = 0;

        for (int x = 0; x < dimensions; x += edgeSize) {
            for (int y = 0; y < dimensions; y += edgeSize) {
                for (int z = 0; z < dimensions; z += edgeSize) {
                    cubeVertices[0] = map[x, y, z];
                    cubeVertices[1] = map[x + 1, y, z];
                    cubeVertices[2] = map[x, y + 1, z];
                    cubeVertices[3] = map[x, y, z + 1];
                    cubeVertices[4] = map[x + 1, y, z + 1];
                    cubeVertices[5] = map[x + 1, y + 1, z];
                    cubeVertices[6] = map[x, y + 1, z + 1];
                    cubeVertices[7] = map[x + 1, y + 1, z + 1];

                    for (int i = 0; i < cubeVertices.Length; i++) {
                        if (cubeVertices[i] < threshhold) {
                            cubeIndex = 1 << i;
                        }
                    }

                    int[] edges = MarchingCubesConstants.triangulationTable[cubeIndex];
                    int[] triangleVertices = new int[edges.Length];

                    for (int i = 0, edgeIntersect = 0; i < edges.Length; i++) {
                        edgeIntersect = 1 << i;
                        if ((MarchingCubesConstants.edgeTable[i] & edgeIntersect) == edgeIntersect) {
                            //triangleVertices[i] = Interpolate(threshhold, , map[x,y,z], )
                        }
                    }
                }
            }
        }

        return new MeshData();
    }

    public static MeshData GenerateMeshData(int dimensions, float[,] map, float heightModifier, AnimationCurve heightCurve) {
        MeshData meshData = new MeshData(dimensions, dimensions);

        //for int y
        for (int z = 0, vertexIndex = 0; z < dimensions; z++) {
            for (int x = 0; x < dimensions; x++, vertexIndex++) {
                meshData.vertexArray[vertexIndex] = new Vector3(x, heightCurve.Evaluate(map[x, z]) * heightModifier, z);
                meshData.uvRays[vertexIndex] = new Vector2(x / (float)dimensions, z / (float)dimensions);

                if (x < dimensions - 1 && z < dimensions - 1) {
                    meshData.AddTriangle(vertexIndex, vertexIndex + dimensions, vertexIndex + 1);
                    meshData.AddTriangle(vertexIndex + 1, vertexIndex + dimensions, vertexIndex + dimensions + 1);
                }
            }
        }

        return meshData;
    }
    #endregion

    void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }
        if (octaves < 0) {
            octaves = 0;
        }
        if (levelOfDetail < 5) {
            levelOfDetail = 5;
        }
        if (mapType == MapType.HeightMap && (mapOffset.z > 0 || mapOffset.z < 0)) {
            mapOffset = new Vector3(mapOffset.x, mapOffset.y, 0);
        }
        while (levelOfDetail % dimensions - 1 != 0) {
            levelOfDetail += 1;
        }
    }

    [System.Serializable]
    public struct TerrainType {
        public string name;
        public float heightThreshold;
        public Color colour;
    }

    #region Data Structs
    public struct MapData {
        public int dimensions;
        public NoiseData3D noiseData;
        public Color[] colorMap;
        public MeshData meshData;

        public MapData(int dimensions, NoiseData3D noiseData, Color[] colorMap, MeshData meshData) {
            this.dimensions = dimensions;
            this.noiseData = noiseData;
            this.colorMap = colorMap;
            this.meshData = meshData;
        }
    }

    public struct MeshData {
        public Vector3[] vertexArray;
        public Vector2[] uvRays;
        public int[] triangleArray;
        public int triangleIndex;

        public MeshData(int meshWidth, int meshHeight) {
            vertexArray = new Vector3[meshWidth * meshHeight];
            uvRays = new Vector2[meshWidth * meshHeight];
            triangleArray = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
            triangleIndex = 0;
        }

        public void AddTriangle(int a, int b, int c) {
            triangleArray[triangleIndex] = a;
            triangleArray[triangleIndex + 1] = b;
            triangleArray[triangleIndex + 2] = c;

            triangleIndex += 3;
        }

        public Mesh CreateMesh() {
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.vertices = vertexArray;
            mesh.triangles = triangleArray;
            mesh.uv = uvRays;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
    #endregion
}