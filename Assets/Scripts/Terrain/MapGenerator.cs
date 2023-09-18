using UnityEngine;
using static Noise;

public class MapGenerator : MonoBehaviour {
    // DrawMode for texture
    public enum DrawMode { ColorMap, TerrainMap };
    public DrawMode drawMode;

    public InterpolateMode interpolateMode;
    public LocalInterpolateMode localInterpolateMode;

    #region NoiseSettings
    public int seed = 1;
    public float scale = 20;
    public const int dimensions = 241;
    public Vector3 mapOffset = Vector3.zero;

    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int octaves = 4;
    #endregion

    #region TextureSettings
    public int meshHeightMultiplier;
    public Color color;
    public AnimationCurve meshHeightCurve;
    public TerrainType[] terrainTypes;
    #endregion

    public MapData GenerateMap(Vector3 ? inputOffset = null, NoiseData ? noiseData = null) {
        Vector3 regionOffset = mapOffset + (inputOffset ?? Vector3.zero);

        noiseData ??= GenerateNoise(seed, scale, dimensions, regionOffset, lacunarity, persistence, octaves, interpolateMode, localInterpolateMode);
        Color[] colorMap = GenerateColorMap(noiseData.Value.map);
        MeshData meshData = GenerateMeshData(dimensions, noiseData.Value.map, meshHeightMultiplier, meshHeightCurve);

        return new MapData(dimensions, noiseData.Value, colorMap, meshData);
    }

    public MapData NormalizeMap(MapData mapData, float globalMin, float globalMax) {
        return GenerateMap(null, Noise.NormalizeNoise(mapData.noiseData, globalMin, globalMax));
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

    private static Color[] GenerateForColorMap(int dimensions, float[,] map, Color color) {
        Color[] colorMap = new Color[dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            //for int z
            for (int x = 0; x < dimensions; x++) {
                //Debug.Log("map[" + x + ", " + y + "] = " + map[x, y]);
                colorMap[y * dimensions + x] = new Color(map[x, y] * color.r, map[x, y] * color.g, map[x, y] * color.b);
            }
        }

        return colorMap;
    }

    private static Color[] GenerateForTerrainMap(int dimensions, float[,] map, TerrainType[] terrainTypes) {
        Color[] colorMap = new Color[dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                //for int z
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
    #endregion

    #region MeshGeneration
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
    }

    #region Data Structs
    public struct MapData {
        public int dimensions;
        public Noise.NoiseData noiseData;
        public Color[] colorMap;
        public MeshData meshData;

        public MapData(int dimensions, Noise.NoiseData noiseData, Color[] colorMap, MeshData meshData) {
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

    [System.Serializable]
    public struct TerrainType {
        public string name;
        public float heightThreshold;
        public Color colour;
    }
}