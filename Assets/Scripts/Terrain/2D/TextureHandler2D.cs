using System.Linq;
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

    public Texture2D GenerateTexture(TerrainType[] terrainTypes, int dimensions, float[] map) {
        TerrainBuffer terrainData = new(terrainTypes.Length);
        TerrainTypeBuffer[] terrainTypeData = terrainTypes.Select(tt => new TerrainTypeBuffer(tt.color, tt.maxHeight)).ToArray();
        ColorBuffer colorData = new(Color.black);

        int kernelID = computeTexture2D.FindKernel("ComputeTexture2D");
        Texture2D output = null;

        using (BufferManager bf = new(computeTexture2D, kernelID)) {
            bf.PrepareBuffer(terrainTypeData, terrainTypeData.Length, TerrainTypeBuffer.size, "terrainTypes");
            bf.PrepareConstantBuffer(terrainData, TerrainBuffer.size, "terrainData");
            bf.PrepareConstantBuffer(colorData, ColorBuffer.size, "colorData");

            output = GenerateTexture(TextureMode.TerrainMap, kernelID, dimensions, map);
        }

        return output;
    }

    public Texture2D GenerateTexture(Color inputColor, int dimensions, float[] map) {
        ColorBuffer colorData = new(inputColor);
        TerrainBuffer terrainData = new(0);
        TerrainTypeBuffer[] terrainTypeData = new TerrainTypeBuffer[1];

        Texture2D output = null;
        int kernelID = computeTexture2D.FindKernel("ComputeTexture2D");

        using (BufferManager bf = new(computeTexture2D, kernelID)) {
            bf.PrepareBuffer(terrainTypeData, terrainTypeData.Length, TerrainTypeBuffer.size, "terrainTypes");
            bf.PrepareConstantBuffer(terrainData, TerrainBuffer.size, "terrainData");
            bf.PrepareConstantBuffer(colorData, ColorBuffer.size, "colorData");

            output = GenerateTexture(TextureMode.TerrainMap, kernelID, dimensions, map);
        }

        return output;
    }

    private Texture2D GenerateTexture(TextureMode mode, int kernelID, int dimensions, float[] inputMap) {
        TextureBuffer textureData = new((int)mode, dimensions);

        Texture2D output = null;

        using (BufferManager bf = new(computeTexture2D, kernelID)) {
            bf.PrepareBuffer(inputMap, inputMap.Length, sizeof(float), "map");
            bf.PrepareConstantBuffer(textureData, TextureBuffer.size, "textureData");

            RenderTexture result = new(dimensions, dimensions, 0, RenderTextureFormat.ARGBHalf);
            result.enableRandomWrite = true; result.Create();

            computeTexture2D.SetTexture(kernelID, "result", result);

            int packets = dimensions / 8;
            computeTexture2D.Dispatch(kernelID, packets, packets, 1);

            output = new(dimensions, dimensions, TextureFormat.RGBAHalf, false); output.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = result; output.ReadPixels(new Rect(0, 0, dimensions, dimensions), 0, 0); output.Apply();

            RenderTexture.active = null;
            result.Release();
        }

        return output;
    }
}