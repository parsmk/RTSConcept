using System;
using UnityEngine;

public class NoiseHandler2D : MonoBehaviour {

    #region Enums
    public enum NoiseInterpolateMode2D { Lerp, Hermite };
    public enum TSmoothMode2D { Fade, SmoothStep };
    public enum NoiseMode2D { Perlin, Simplex, Worley };
    #endregion

    #region Structs

    public struct NoiseData2D {
        public int dimensions;
        public float min, max;
        public float[] map;

        public NoiseData2D(int dimensions, float localMin, float localMax, float[] map) {
            this.dimensions = dimensions;
            this.min = localMin;
            this.max = localMax;
            this.map = map;
        }
    }

    private struct NoiseBuffer {
        public static int size = sizeof(float) * 2 + sizeof(int) * 4;
        public Vector2 offset;
        public int dimensions;
        public int noiseMode;
        public int interpolateMode;
        public int fractal;

        public NoiseBuffer(Vector2 offset, int dimensions, int noiseMode, int interpolateMode, int fractal) {
            this.offset = offset;
            this.dimensions = dimensions;
            this.noiseMode = noiseMode;
            this.interpolateMode = interpolateMode;
            this.fractal = fractal;
        }
    }

    private struct PerlinBuffer {
        public static int size = sizeof(int) * 1;
        public int tSmoothMode;

        public PerlinBuffer(int tSmoothMode) {
            this.tSmoothMode = tSmoothMode;
        }
    }

    private struct WorleyBuffer {
        public static int size = sizeof(int) * 1;
        int seedPointCount;

        public WorleyBuffer(int seedPointCount) {
            this.seedPointCount = seedPointCount;
        }
    }

    private struct FractalBuffer {
        public static int size = sizeof(int) * 1 + sizeof(float) * 3;
        public int octaves;
        public float scale;
        public float persistence;
        public float lacunarity;

        public FractalBuffer(int octaves, float scale, float persistence, float lacunarity) {
            this.octaves = octaves;
            this.scale = scale;
            this.persistence = persistence;
            this.lacunarity = lacunarity;
        }
    }

    private struct NormalizeBuffer {
        public static int size = sizeof(float) * 2;
        public float min, max;

        public NormalizeBuffer(float min, float max) {
            this.min = min;
            this.max = max;
        }
    }

    #endregion

    [Header("ComputeShaders")]
    public ComputeShader computeNoise2D;

    [Header("NoiseSettings")]
    public readonly int noiseDimensions = 128;
    public bool fractal = true;
    public NoiseMode2D noiseMode;
    public NoiseInterpolateMode2D interpolateMode;
    public Vector2 mapOffset = Vector2.zero;
    public PerlinSettings perlinSettings = PerlinSettings.Default();
    public FractalSettings fractalSettings = FractalSettings.Default();

    private static int[] permutationTable;

    public NoiseData2D GenerateNoise(NoiseMode2D noiseMode, Vector2 offset) {
        if (noiseMode == NoiseMode2D.Worley) { 
            throw new ArgumentException("Need seedpoints for Worley"); 
        }

        NoiseBuffer noiseData = new(mapOffset + offset, noiseDimensions, (int)noiseMode, (int)interpolateMode, fractal ? 1 : 0);
        permutationTable ??= GeneratePermutationTable();
        PerlinBuffer perlinData = new((int)perlinSettings.tSmoothMode);
        FractalBuffer fractalData = new(fractalSettings.octaves, fractalSettings.scale, fractalSettings.persistence, fractalSettings.lacunarity);
        Vector2[] octaveOffsets = GenerateOctaveOffsets();
        WorleyBuffer worleyData = new(0);
        Vector2[] seedPoints = new Vector2[1];

        int kernelID = computeNoise2D.FindKernel("ComputeNoise");
        float[] map = new float[noiseDimensions * noiseDimensions];

        using (BufferManager bM = new(computeNoise2D, kernelID)) {
            bM.PrepareConstantBuffer(noiseData, NoiseBuffer.size, "noiseData");
            bM.PrepareConstantBuffer(perlinData, PerlinBuffer.size, "perlinData");
            bM.PrepareBuffer(permutationTable, permutationTable.Length, sizeof(int), "permutationTable");

            bM.PrepareConstantBuffer(worleyData, WorleyBuffer.size, "worleyData");
            bM.PrepareBuffer(seedPoints, seedPoints.Length, sizeof(float) * 2, "seedPoints");

            bM.PrepareConstantBuffer(fractalData, FractalBuffer.size, "fractalData");
            bM.PrepareBuffer(octaveOffsets, octaveOffsets.Length, sizeof(float) * 2, "octaveOffsets");

            ComputeBuffer resultBuffer = bM.PrepareOutputBuffer(map.Length, sizeof(float), "result");

            int packets = noiseDimensions / 8;
            computeNoise2D.Dispatch(kernelID, packets, packets, 1);

            resultBuffer.GetData(map);
        }

        // Min Max Height
        (float min, float max) = ComputeMinMax(map);

        return new NoiseData2D(noiseDimensions, min, max, map);
    }

    public NoiseData2D GenerateNoise(NoiseMode2D noiseMode, Vector2[] seedPoints) {
        if (noiseMode != NoiseMode2D.Worley || seedPoints == null || seedPoints.Length < 1) {
            throw new ArgumentException("Seed points are needed to generate Worley noise.");
        }

        NoiseBuffer noiseData = new(Vector2.zero, noiseDimensions, (int)noiseMode, (int)interpolateMode, fractal ? 1 : 0);
        permutationTable ??= GeneratePermutationTable();
        PerlinBuffer perlinData = new(0);
        FractalBuffer fractalData = new(0, 0, 0, 0);
        Vector2[] octaveOffsets = new Vector2[] { Vector2.zero };
        WorleyBuffer worleyData = new(seedPoints.Length);

        int kernelID = computeNoise2D.FindKernel("ComputeNoise");
        float[] map = new float[noiseDimensions * noiseDimensions];

        using (BufferManager bM = new(computeNoise2D, kernelID)) {
            bM.PrepareConstantBuffer(noiseData, NoiseBuffer.size, "noiseData");
            bM.PrepareConstantBuffer(perlinData, PerlinBuffer.size, "perlinData");
            bM.PrepareBuffer(permutationTable, permutationTable.Length, sizeof(int), "permutationTable");

            bM.PrepareConstantBuffer(worleyData, WorleyBuffer.size, "worleyData");
            bM.PrepareBuffer(seedPoints, seedPoints.Length, sizeof(float) * 2, "seedPoints");

            bM.PrepareConstantBuffer(fractalData, FractalBuffer.size, "fractalData");
            bM.PrepareBuffer(octaveOffsets, octaveOffsets.Length, sizeof(float) * 2 , "octaveOffsets");

            ComputeBuffer resultBuffer = bM.PrepareOutputBuffer(map.Length, sizeof(float), "result");

            int packets = noiseDimensions / 8;
            computeNoise2D.Dispatch(kernelID, packets, packets, 1);

            resultBuffer.GetData(map);
        }

        // Min Max Height
        (float min, float max) = ComputeMinMax(map);

        return new NoiseData2D(noiseDimensions, min, max, map);
    }

    public NoiseData2D NormalizeNoise(NoiseData2D inputNoiseData, float min, float max) {
        // Prepare Buffer Data
        NoiseBuffer noiseData = new(Vector2.zero, noiseDimensions, (int)noiseMode, (int)interpolateMode, fractal ? 1 : 0);
        PerlinBuffer perlinData = new((int)perlinSettings.tSmoothMode);
        NormalizeBuffer normalizeData = new(min, max);

        int kernelID = computeNoise2D.FindKernel("NormalizeNoise");
        float[] map = new float[noiseDimensions * noiseDimensions];

        using (BufferManager bM = new(computeNoise2D, kernelID)) {
            bM.PrepareConstantBuffer(noiseData, NoiseBuffer.size, "noiseData");
            bM.PrepareConstantBuffer(perlinData, PerlinBuffer.size, "perlinData");
            bM.PrepareConstantBuffer(normalizeData, NormalizeBuffer.size, "normalizeData");
            bM.PrepareBuffer(inputNoiseData.map, inputNoiseData.map.Length, sizeof(float), "oldMap");

            ComputeBuffer resultBuffer = bM.PrepareOutputBuffer(map.Length, sizeof(float), "result");

            int packets = noiseDimensions / 8;
            computeNoise2D.Dispatch(kernelID, packets, packets, 1);

            resultBuffer.GetData(map);
        }

        return new NoiseData2D(noiseDimensions, min, max, map);
    }

    #region Helper Functions
    private (float min, float max) ComputeMinMax(float[] map) {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int y = 0; y < noiseDimensions; y++) {
            for (int x = 0; x < noiseDimensions; x++) {
                int index = TerrainUtils.Index2D(x, y, noiseDimensions);
                min = Mathf.Min(map[index], min);
                max = Mathf.Max(map[index], max);
            }
        }

        return (min, max);
    } 

    private Vector2[] GenerateOctaveOffsets() {
        Vector2[] output = new Vector2[fractalSettings.octaves];
        System.Random prng = new System.Random(fractalSettings.seed);

        for (int i = 0; i < fractalSettings.octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + mapOffset.x;
            float offsetY = prng.Next(-100000, 100000) + mapOffset.y;
            output[i] = new Vector2(offsetX, offsetY);
        }

        return output;
    }

    private int[] GeneratePermutationTable() {
        int[] output;
        output = new int[512];

        for (int i = 0; i < 256; i++) {
            output[i] = i;
        }

        for (int i = 256; i > 0; i--) {
            int index = UnityEngine.Random.Range(0, i);

            (output[index], output[i]) = (output[i], output[index]);
        }

        for (int i = 0; i < 256; i++) {
            output[i + 256] = output[i];
        }

        return output;
    }

    #endregion

    private void OnValidate() {
        // NoiseSettings
        if (fractalSettings.lacunarity < 1) { fractalSettings.lacunarity = 1; }
        if (fractalSettings.octaves < 1) { fractalSettings.octaves = 1; }

    }
}
