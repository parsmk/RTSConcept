using UnityEngine;

public class MapGenerator : MonoBehaviour {
    public enum DrawMode { ColorMap, TerrainMap };

    #region NoiseSettings
    [Header("Noise")]
    public NoiseGenerator.NoiseInterpolateMode noiseInterpolateMode;
    public NoiseGenerator.NoiseLocalInterpolateMode noiseLocalInterpolateMode;

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
    //[HideInInspector]
    public int levelOfDetail = 5;
    [Range(0, 1)]
    public float threshhold = 0.5f;
    public AnimationCurve animationCurve;

    //2D
    //[HideInInspector]
    public int heightModifier = 50;
    #endregion

    #region TextureSettings
    [Header("Texture")]
    public DrawMode drawMode;
    //[HideInInspector]
    public Color color;
    //[HideInInspector]
    public TerrainType[] terrainTypes;
    #endregion

    public MapData2D GenerateMap2D(Vector2? inputOffset = null, NoiseGenerator.NoiseData2D? noiseData = null) {
        Vector2 regionOffset = (Vector2)mapOffset + (inputOffset ?? Vector2.zero);

        noiseData ??= NoiseGenerator.GenerateNoise(
                                seed,
                                scale,
                                dimensions,
                                regionOffset,
                                lacunarity,
                                persistence,
                                octaves,
                                noiseInterpolateMode,
                                noiseLocalInterpolateMode
                            );
        MeshData meshData = GenerateMeshData(dimensions, noiseData.Value.map, heightModifier, animationCurve);
        Color[] colorMap = GenerateColorMap(noiseData.Value.map);

        return new MapData2D(dimensions, noiseData.Value, colorMap, meshData);
    }
    public MapData3D GenerateMap3D(Vector3? inputOffset = null, NoiseGenerator.NoiseData3D? noiseData = null) {
        Vector3 regionOffset = mapOffset + (inputOffset ?? Vector3.zero);

        noiseData ??= NoiseGenerator.GenerateNoise(
                                seed,
                                scale,
                                dimensions,
                                regionOffset,
                                lacunarity,
                                persistence,
                                octaves,
                                noiseInterpolateMode,
                                noiseLocalInterpolateMode
                            );
        MeshData meshData = MarchCubes(dimensions, noiseData.Value.map);
        Color[] colorMap = GenerateColorMap(noiseData.Value.map);

        return new MapData3D(dimensions, noiseData.Value, colorMap, meshData);
    }

    public MapData2D NormalizeMap(MapData2D mapData, float globalMin, float globalMax) {
        return GenerateMap2D(null, NoiseGenerator.NormalizeNoise(mapData.noiseData, globalMin, globalMax));
    }
    public MapData3D NormalizeMap(MapData3D mapData, float globalMin, float globalMax) {
        return GenerateMap3D(null, NoiseGenerator.NormalizeNoise(mapData.noiseData, globalMin, globalMax));
    }

    #region ColorMapGeneration
    private Color[] GenerateColorMap(float[,] map) {
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

    private static Color[] GenerateForColorMap(int dimensions, float[,] map, Color color) {
        Color[] colorMap = new Color[dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                colorMap[y * dimensions + x] = new Color(map[x, y] * color.r, map[x, y] * color.g, map[x, y] * color.b);
            }
        }

        return colorMap;
    }
    private static Color[] GenerateForColorMap(int dimensions, float[,,] map, Color color) {
        Color[] colorMap = new Color[dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                for (int z = 0; z < dimensions; z++)  {
                    colorMap[(y + z * dimensions) * dimensions + x] = new Color(map[x, y, z] * color.r, map[x, y, z] * color.g, map[x, y, z] * color.b);
                }
            }
        }

        return colorMap;
    }

    private static Color[] GenerateForTerrainMap(int dimensions, float[,] map, TerrainType[] terrainTypes) {
        Color[] colorMap = new Color[dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                foreach (TerrainType terrainType in terrainTypes) {
                    if (map[x, y] <= terrainType.heightThreshold) {
                        colorMap[y * dimensions + x] = terrainType.colour;
                        break;
                    }
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
                            colorMap[(y + z * dimensions) * dimensions + x] = terrainType.colour;
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
    public static MeshData GenerateMeshData(int dimensions, float[,] map, float heightModifier, AnimationCurve animationCurve) {
        MeshData meshData = new MeshData(dimensions, dimensions);

        //for int y
        for (int y = 0, vertexIndex = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++, vertexIndex++) {
                meshData.vertexArray[vertexIndex] = new Vector3(x, animationCurve.Evaluate(map[x, y]) * heightModifier, y);
                meshData.uvRays[vertexIndex] = new Vector2(x / (float)dimensions, y / (float)dimensions);

                if (x < dimensions - 1 && y < dimensions - 1) {
                    meshData.AddTriangle(vertexIndex, vertexIndex + dimensions, vertexIndex + 1);
                    meshData.AddTriangle(vertexIndex + 1, vertexIndex + dimensions, vertexIndex + dimensions + 1);
                }
            }
        }

        return meshData;
    }
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
    #endregion

    [System.Serializable]
    public struct TerrainType {
        public string name;
        public float heightThreshold;
        public Color colour;
    }

    #region Data Structs
    public struct MapData2D {
        public int dimensions;
        public NoiseGenerator.NoiseData2D noiseData;
        public Color[] colorMap;
        public MeshData meshData;

        public MapData2D(int dimensions, NoiseGenerator.NoiseData2D noiseData, Color[] colorMap, MeshData meshData) {
            this.dimensions = dimensions;
            this.noiseData = noiseData;
            this.colorMap = colorMap;
            this.meshData = meshData;
        }

        public static implicit operator MapData2D(MapData3D mapData3D) {
            return new MapData2D(mapData3D.dimensions, mapData3D.noiseData, mapData3D.colorMap, mapData3D.meshData);
        }
    }
    public struct MapData3D {
        public int dimensions;
        public NoiseGenerator.NoiseData3D noiseData;
        public Color[] colorMap;
        public MeshData meshData;

        public MapData3D(int dimensions, NoiseGenerator.NoiseData3D noiseData, Color[] colorMap, MeshData meshData) {
            this.dimensions = dimensions;
            this.noiseData = noiseData;
            this.colorMap = colorMap;
            this.meshData = meshData;
        }

        public static implicit operator MapData3D(MapData2D mapData2D) {
            return new MapData3D(mapData2D.dimensions, mapData2D.noiseData, mapData2D.colorMap, mapData2D.meshData);
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

    void OnValidate() {
        // NoiseSettings
        if (lacunarity < 1) { lacunarity = 1; }
        if (octaves < 0) { octaves = 0; }

        // MeshSettings
        if (levelOfDetail < 5) { levelOfDetail = 5; }
        if (heightModifier < 0) { heightModifier = 0; }
    }
}