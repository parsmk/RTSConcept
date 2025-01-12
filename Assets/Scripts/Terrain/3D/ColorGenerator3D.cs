using UnityEngine;

public static class ColorGenerator3D {
    public enum DrawMode { ColorMap, TerrainMap };

    public static Color[] GenerateForColorMap(int dimensions, float[,,] map, Color color) {
        Color[] colorMap = new Color[dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                for (int z = 0; z < dimensions; z++) {
                    colorMap[(y + z * dimensions) * dimensions + x] = new Color(map[x, y, z] * color.r, map[x, y, z] * color.g, map[x, y, z] * color.b);
                }
            }
        }

        return colorMap;
    }

    public static Color[] GenerateForTerrainMap(int dimensions, float[,,] map, TerrainType[] terrainTypes) {
        Color[] colorMap = new Color[dimensions * dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                for (int z = 0; z < dimensions; z++) {
                    foreach (TerrainType terrainType in terrainTypes) {
                        if (map[x, y, z] <= terrainType.maxHeight) {
                            colorMap[(y + z * dimensions) * dimensions + x] = terrainType.color;
                            break;
                        }
                    }
                }
            }
        }

        return colorMap;
    }
}
