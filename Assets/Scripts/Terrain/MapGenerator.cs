using UnityEngine;

public class MapGenerator : MonoBehaviour {
    public enum DrawMode { ColorMap, TerrainMap };

    #region NoiseSettings
    [Header("Noise")]
    public NoiseGenerator.NoiseMode noiseMode;
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
                                noiseMode,
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
                                noiseMode,
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

        for (int y = 0, vertexIndex = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++, vertexIndex++) {
                meshData.vertexArray[vertexIndex] = new Vector3(x, animationCurve.Evaluate(map[x, y]) * heightModifier, y);
                meshData.uvRays[vertexIndex] = new Vector2(x / (float)dimensions, y / (float)dimensions);

                if (x < dimensions - 1 && y < dimensions - 1) {
                    meshData.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + dimensions);
                    meshData.AddTriangle(vertexIndex + 1, vertexIndex + dimensions + 1, vertexIndex + dimensions);
                }
            }
        }

        return meshData;
    }
    
    public MeshData MarchCubes(int dimensions, float[,,] map) {
        MeshData meshData = new MeshData(dimensions, dimensions, dimensions);
        Vector3[] vertexArray = new Vector3[dimensions * dimensions * dimensions];
        int[] triangleArray = new int[dimensions * dimensions * dimensions];
        int vertIndex = 0, triIndex = 0;

        int edgeSize = dimensions / levelOfDetail;
        (Vector3 cord, float value)[] cubeVertices = new (Vector3 cord, float value)[8];

        for (int x = 0; x < dimensions; x += edgeSize) {
            for (int y = 0; y < dimensions; y += edgeSize) {
                for (int z = 0; z < dimensions; z += edgeSize) {
                    int cubeIndex = 0;

                    cubeVertices[0] = (new Vector3(x, y, z), map[x, y, z]);
                    cubeVertices[1] = (new Vector3(x + edgeSize, y, z), map[x, y, z]);
                    cubeVertices[2] = (new Vector3(x, y + edgeSize, z), map[x, y + edgeSize, z]);
                    cubeVertices[3] = (new Vector3(x, y, z + edgeSize),  map[x, y, z + edgeSize]);
                    cubeVertices[4] = (new Vector3(x + edgeSize, y, z + edgeSize), map[x + edgeSize, y, z + edgeSize]);
                    cubeVertices[5] = (new Vector3(x + edgeSize, y + edgeSize, z), map[x + edgeSize, y + edgeSize, z]);
                    cubeVertices[6] = (new Vector3(x, y + edgeSize, z + edgeSize), map[x, y + edgeSize, z + edgeSize]);
                    cubeVertices[7] = (new Vector3(x + edgeSize, y + edgeSize, z + edgeSize), map[x + edgeSize, y + edgeSize, z + edgeSize]);

                    for (int i = 0; i < cubeVertices.Length; i++) {
                        if (cubeVertices[i].value < threshhold) {
                            cubeIndex |= 1 << i;
                        }
                    }

                    int edgesIntersected = MarchingCubesConstants.edgeTable[cubeIndex];
                        
                    if (edgesIntersected == 0)
                        continue;

                    for (int edge = 1; edge <= 0xffc; edge <<= 1) {
                        if ((edgesIntersected & edge) != 0) {
                            float valueVertA = cubeVertices[MarchingCubesConstants.edgeIndices[edge].a].value;
                            float valueVertB = cubeVertices[MarchingCubesConstants.edgeIndices[edge].b].value;

                            Vector3 cordVertA = cubeVertices[MarchingCubesConstants.edgeIndices[edge].a].cord;
                            Vector3 cordVertB = cubeVertices[MarchingCubesConstants.edgeIndices[edge].b].cord;

                            float factor = (threshhold - valueVertA) / (valueVertB - valueVertA);

                            float outputX = cordVertA.x + factor * (cordVertB.x - cordVertA.x);
                            float outputY = cordVertA.y + factor * (cordVertB.y - cordVertA.y);
                            float outputZ = cordVertA.z + factor * (cordVertB.z - cordVertA.z);

                            //TODO: Populate MeshData.VertexArray
                            vertexArray[vertIndex] = new Vector3(outputX, outputY, outputZ); vertIndex++;
                        }
                    }


                    for (int i = 0; MarchingCubesConstants.triangulationTable[cubeIndex][i] != -1; i += 3) {
                        int a = MarchingCubesConstants.triangulationTable[cubeIndex][i];
                        int b = MarchingCubesConstants.triangulationTable[cubeIndex][i + 1];
                        int c = MarchingCubesConstants.triangulationTable[cubeIndex][i + 2];

                        //TODO: Populate MeshData.TriangleArray
                        triangleArray[triIndex] = triIndex + a; triIndex++;
                        triangleArray[triIndex] = triIndex + b; triIndex++;
                        triangleArray[triIndex] = triIndex + c; triIndex++;
                    }

                }
            }
        }

        return meshData;
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
        public MeshData(int meshWidth, int meshHeight, int meshDepth) {
            vertexArray = new Vector3[meshWidth * meshHeight * meshDepth];
            uvRays = new Vector2[meshWidth * meshHeight * meshDepth];
            triangleArray = new int[(meshWidth - 1) * (meshHeight - 1) * (meshDepth - 1) * 6];
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
        if (octaves < 1) { octaves = 1; }

        // MeshSettings
        if (levelOfDetail < 5) { levelOfDetail = 5; }
        if (heightModifier < 0) { heightModifier = 0; }
    }
}