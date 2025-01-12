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
        public static int size = sizeof(float) * 2 + sizeof(int) * 3;
        public Vector2 offset;
        public int dimensions;
        public int noiseMode;
        public int interpolateMode;

        public NoiseBuffer(Vector2 offset, int dimensions, int noiseMode, int interpolateMode) {
            this.offset = offset;
            this.dimensions = dimensions;
            this.noiseMode = noiseMode;
            this.interpolateMode = interpolateMode;
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
    public NoiseMode2D noiseMode;
    public NoiseInterpolateMode2D interpolateMode;
    public Vector2 mapOffset = Vector2.zero;
    public PerlinSettings perlinSettings = PerlinSettings.Default();
    public FractalSettings fractalSettings = FractalSettings.Default();

    private static int[] permutationTable;

    public NoiseData2D GenerateNoise2D(NoiseMode2D noiseMode, Vector2 offset) {
        // Prepare Buffer Data
        NoiseBuffer noiseData = new NoiseBuffer(mapOffset + offset, noiseDimensions, (int)noiseMode, (int)interpolateMode);
        permutationTable ??= GeneratePermutationTable();

        PerlinBuffer perlinData = new PerlinBuffer((int)perlinSettings.tSmoothMode);

        FractalBuffer fractalData = new FractalBuffer(fractalSettings.octaves, fractalSettings.scale, fractalSettings.persistence, fractalSettings.lacunarity);
        Vector2[] octaveOffsets = GenerateOctaveOffsets();

        WorleyBuffer worleyData = new WorleyBuffer(0);
        Vector2[] seedPoints = new Vector2[1];

        // Create Buffers
        ComputeBuffer resultBuffer = new ComputeBuffer(noiseDimensions * noiseDimensions, sizeof(float));
        ComputeBuffer noiseBuffer = new ComputeBuffer(1, NoiseBuffer.size, ComputeBufferType.Constant);
        ComputeBuffer permutationBuffer = new ComputeBuffer(permutationTable.Length, sizeof(int));

        ComputeBuffer perlinBuffer = new ComputeBuffer(1, PerlinBuffer.size, ComputeBufferType.Constant);

        ComputeBuffer fractalBuffer = new ComputeBuffer(1, FractalBuffer.size, ComputeBufferType.Constant);
        ComputeBuffer octaveBuffer = new ComputeBuffer(fractalSettings.octaves, sizeof(float) * 2);

        ComputeBuffer worleyBuffer = new ComputeBuffer(1, WorleyBuffer.size, ComputeBufferType.Constant);
        ComputeBuffer seedBuffer = new ComputeBuffer(1, sizeof(float) * 2);

        // Set Buffer Data
        noiseBuffer.SetData(new[] { noiseData });
        permutationBuffer.SetData(permutationTable);

        perlinBuffer.SetData(new[] { perlinData });

        fractalBuffer.SetData(new[] { fractalData });
        octaveBuffer.SetData(octaveOffsets);

        worleyBuffer.SetData(new[] { worleyData });
        seedBuffer.SetData(seedPoints);

        // Prepare Buffers for Dispatch
        int kernelID = computeNoise2D.FindKernel("ComputeNoise");
        computeNoise2D.SetBuffer(kernelID, "result", resultBuffer);
        computeNoise2D.SetConstantBuffer("noiseData", noiseBuffer, 0, NoiseBuffer.size);
        computeNoise2D.SetBuffer(kernelID, "permutationTable", permutationBuffer);

        computeNoise2D.SetConstantBuffer("perlinData", perlinBuffer, 0, PerlinBuffer.size);

        computeNoise2D.SetConstantBuffer("fractalData", fractalBuffer, 0, FractalBuffer.size);
        computeNoise2D.SetBuffer(kernelID, "octaveOffsets", octaveBuffer);

        computeNoise2D.SetConstantBuffer("worleyData", worleyBuffer, 0, WorleyBuffer.size);
        computeNoise2D.SetBuffer(kernelID, "seedPoints", seedBuffer);

        // Dispatch
        int packets = noiseDimensions / 8;
        computeNoise2D.Dispatch(kernelID, packets, packets, 1);

        // Retrieve data
        float[] map = new float[noiseDimensions * noiseDimensions];
        resultBuffer.GetData(map);

        // Release Resources
        resultBuffer.Release();
        noiseBuffer.Release();
        permutationBuffer.Release();

        perlinBuffer.Release();

        fractalBuffer.Release();
        octaveBuffer.Release();

        worleyBuffer.Release();
        seedBuffer.Release();

        // 1D -> 2D and find Min Max Height
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int y = 0; y < noiseDimensions; y++) {
            for (int x = 0; x < noiseDimensions; x++) {
                min = Mathf.Min(map[y * noiseDimensions + x], min);
                max = Mathf.Max(map[y * noiseDimensions + x], max);
            }
        }

        return new NoiseData2D(noiseDimensions, min, max, map);
    }

    public NoiseData2D GenerateNoise2D(NoiseMode2D noiseMode, Vector2[] seedPoints) {
        NoiseData2D output = new NoiseData2D(0, 0, 0, null);
        if (noiseMode != NoiseMode2D.Worley || seedPoints == null || seedPoints.Length < 1) {
            throw new ArgumentException("Seed points are needed to generate Worley noise.");
        }

        return output;
    }
    
    public NoiseData2D GenerateNoise2D(NoiseMode2D noiseMode) {
        if (noiseMode == NoiseMode2D.Worley)
            throw new ArgumentException("Seed points are needed to generate Worley noise.");

        return GenerateNoise2D(noiseMode, Vector2.zero);
    }

    public NoiseData2D NormalizeNoise(NoiseData2D inputNoiseData, float min, float max) {

        // Prepare Buffer Data
        NoiseBuffer noiseData = new NoiseBuffer(Vector2.zero, noiseDimensions, (int)noiseMode, (int)interpolateMode);
        PerlinBuffer perlinData = new PerlinBuffer((int)perlinSettings.tSmoothMode);
        NormalizeBuffer normalizeData = new NormalizeBuffer(min, max);

        // Create Buffers
        ComputeBuffer resultBuffer = new ComputeBuffer(noiseDimensions * noiseDimensions, sizeof(float));
        ComputeBuffer noiseBuffer = new ComputeBuffer(1, NoiseBuffer.size, ComputeBufferType.Constant);
        ComputeBuffer perlinBuffer = new ComputeBuffer(1, PerlinBuffer.size, ComputeBufferType.Constant);
        ComputeBuffer normalizeBuffer = new ComputeBuffer(1, NormalizeBuffer.size, ComputeBufferType.Constant);
        ComputeBuffer oldMapBuffer = new ComputeBuffer(noiseDimensions * noiseDimensions, sizeof(float));

        // Set necessary Buffer data
        noiseBuffer.SetData(new[] { noiseData });
        perlinBuffer.SetData(new[] { perlinData });
        normalizeBuffer.SetData(new[] { normalizeData });
        oldMapBuffer.SetData(inputNoiseData.map);

        // Prepare Buffers for Dispatch
        int kernelID = computeNoise2D.FindKernel("NormalizeNoise");
        computeNoise2D.SetBuffer(kernelID, "result", resultBuffer);
        computeNoise2D.SetConstantBuffer("noiseData", noiseBuffer, 0, NoiseBuffer.size);
        computeNoise2D.SetConstantBuffer("perlinData", perlinBuffer, 0, PerlinBuffer.size);
        computeNoise2D.SetConstantBuffer("normalizeData", normalizeBuffer, 0, NormalizeBuffer.size);
        computeNoise2D.SetBuffer(kernelID, "oldMap", oldMapBuffer);

        // Dispatch
        int packets = noiseDimensions / 8;
        computeNoise2D.Dispatch(kernelID, packets, packets, 1);

        // Retrieve Data
        float[] newMap = new float[noiseDimensions * noiseDimensions];
        resultBuffer.GetData(newMap);

        // Release Resources
        resultBuffer.Release();
        noiseBuffer.Release();
        perlinBuffer.Release();
        normalizeBuffer.Release();
        oldMapBuffer.Release();

        return new NoiseData2D(noiseDimensions, min, max, newMap);
    }

    #region Helper Functions
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

            int temp = output[i];
            output[i] = output[index];
            output[index] = temp;
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
