using System;

[System.Serializable]
public struct FractalSettings {
    public int seed;
    public float scale;
    public float persistence;
    public float lacunarity;
    public int octaves;

    public FractalSettings(
        int seed, 
        float scale, 
        float persistence, 
        float lacunarity, 
        int octaves
    
    ) {
        this.seed = seed;
        this.scale = scale;
        this.persistence = persistence;
        this.lacunarity = lacunarity;
        this.octaves = octaves;
    }

    public static FractalSettings Default() {
        int seed = 1;
        float scale = 100;
        float persistence = 0.5f;
        float lacunarity = 2f;
        int octaves = 4;

        return new FractalSettings(seed, scale, persistence, lacunarity, octaves);
    }
}

[Serializable]
public struct PerlinSettings {
    public NoiseHandler2D.TSmoothMode2D tSmoothMode;

    public PerlinSettings(
        int tSmoothMode
    ) {
        this.tSmoothMode = (NoiseHandler2D.TSmoothMode2D)tSmoothMode;
    }
    public static PerlinSettings Default() {
        return new PerlinSettings(0);
    }
}

[Serializable]
public struct MeshSettings {}

[Serializable]
public struct TextureSettings {}