using UnityEngine;

public class TextureHandler2D : MonoBehaviour {
    public enum TextureMode { ColorMap, TerrainMap }

    #region Structs
    private struct TextureBuffer {
        public static int size = sizeof(int) * 2;
        public int mode;
        public int dimensions;

        public TextureBuffer(int mode, int dimensions) {
            this.mode = mode;
            this.dimensions = dimensions;
        }
    }

    private struct TerrainBuffer {
        public static int size = sizeof(int) * 1;
        public int count;

        public TerrainBuffer(int count) { this.count = count; }
    }

    private struct ColorBuffer {
        public static int size = sizeof(float) * 4;
        public Color color;

        public ColorBuffer(Color color) {
            this.color = color;
        }
    }

    private struct TerrainTypeBuffer {
        public static int size = sizeof(float) * 5;
        public Color color;
        public float maxHeight;

        public TerrainTypeBuffer(Color color, float maxHeight) {
            this.color = color;
            this.maxHeight = maxHeight;
        }
    }
    #endregion

    public ComputeShader computeTexture2D;
    public TextureMode textureMode;
    public Material material;
    public Color color;

    public Texture2D ComputeTexture(TerrainType[] terrainTypes, int dimensions, float[] map) {
        TerrainBuffer terrainData = new TerrainBuffer(terrainTypes.Length);
        TerrainTypeBuffer[] terrainTypesData = new TerrainTypeBuffer[terrainTypes.Length];
        for (int i = 0; i < terrainTypes.Length; i++) { terrainTypesData[i] = new(terrainTypes[i].color, terrainTypes[i].maxHeight); }
        ColorBuffer colorData = new ColorBuffer(Color.black);

        ComputeBuffer terrainBuffer = new ComputeBuffer(1, TerrainBuffer.size, ComputeBufferType.Constant);
        ComputeBuffer terrainTypeBuffer = new ComputeBuffer(terrainTypesData.Length, TerrainTypeBuffer.size);
        ComputeBuffer colorBuffer = new ComputeBuffer(1, ColorBuffer.size, ComputeBufferType.Constant);

        terrainBuffer.SetData(new[] { terrainData });
        terrainTypeBuffer.SetData(terrainTypesData);
        colorBuffer.SetData(new[] { colorData });

        int kernelID = computeTexture2D.FindKernel("ComputeTexture2D");
        computeTexture2D.SetBuffer(kernelID, "terrainTypes", terrainTypeBuffer);
        computeTexture2D.SetConstantBuffer("terrainData", terrainBuffer, 0, TerrainBuffer.size);
        computeTexture2D.SetConstantBuffer("colorData", colorBuffer, 0, ColorBuffer.size);

        Texture2D output = ComputeTexture(TextureMode.TerrainMap, kernelID, dimensions, map);

        terrainTypeBuffer.Release();
        terrainBuffer.Release();
        colorBuffer.Release();

        return output;
    }

    public Texture2D ComputeTexture(Color inputColor, int dimensions, float[] map) {
        ColorBuffer colorData = new ColorBuffer(inputColor);
        TerrainBuffer terrainData = new TerrainBuffer(0);
        TerrainTypeBuffer[] terrainTypeData = new TerrainTypeBuffer[1];

        ComputeBuffer colorBuffer = new ComputeBuffer(1, ColorBuffer.size, ComputeBufferType.Constant);
        ComputeBuffer terrainBuffer = new ComputeBuffer(1, TerrainBuffer.size, ComputeBufferType.Constant);
        ComputeBuffer terrainTypeBuffer = new ComputeBuffer(1, TerrainTypeBuffer.size);

        colorBuffer.SetData(new[] { colorData });
        terrainBuffer.SetData(new[] { terrainData });
        terrainTypeBuffer.SetData(terrainTypeData);

        int kernelID = computeTexture2D.FindKernel("ComputeTexture2D");
        computeTexture2D.SetConstantBuffer("colorData", colorBuffer, 0, ColorBuffer.size);
        computeTexture2D.SetConstantBuffer("terrainData", terrainBuffer, 0, TerrainBuffer.size);
        computeTexture2D.SetBuffer(kernelID, "terrainTypes", terrainTypeBuffer);

        Texture2D output = ComputeTexture(TextureMode.ColorMap, kernelID, dimensions, map);

        colorBuffer.Release();
        terrainBuffer.Release();
        terrainTypeBuffer.Release();

        return output;
    }

    private Texture2D ComputeTexture(TextureMode mode, int kernelID, int dimensions, float[] inputMap) {
        TextureBuffer textureData = new TextureBuffer((int)mode, dimensions);

        RenderTexture resultBuffer = new RenderTexture(dimensions, dimensions, 0, RenderTextureFormat.ARGBHalf); 
        resultBuffer.enableRandomWrite = true; resultBuffer.Create();
        ComputeBuffer mapBuffer = new ComputeBuffer(dimensions * dimensions, sizeof(float));
        ComputeBuffer textureBuffer = new ComputeBuffer(1, TextureBuffer.size, ComputeBufferType.Constant);

        mapBuffer.SetData(inputMap);
        textureBuffer.SetData(new[] { textureData });

        computeTexture2D.SetTexture(kernelID, "result", resultBuffer);
        computeTexture2D.SetBuffer(kernelID, "map", mapBuffer);
        computeTexture2D.SetConstantBuffer("textureData", textureBuffer, 0, TextureBuffer.size);

        int packets = dimensions / 16;
        computeTexture2D.Dispatch(kernelID, packets, packets, 1);

        Texture2D output = new Texture2D(dimensions, dimensions, TextureFormat.RGBAHalf, false);
        RenderTexture.active = resultBuffer; output.ReadPixels(new Rect(0, 0, dimensions, dimensions), 0, 0); output.Apply();

        RenderTexture.active = null;
        resultBuffer.Release();
        mapBuffer.Release();
        textureBuffer.Release();

        return output;
    }
}