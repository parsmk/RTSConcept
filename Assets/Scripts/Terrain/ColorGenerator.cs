using UnityEngine;

public static class ColorGenerator {
    public enum DrawMode { ColorMap, TerrainMap };

    public static Color[] GenerateForColorMap(int dimensions, float[,] map, Color color) {
        Color[] colorMap = new Color[dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                colorMap[y * dimensions + x] = new Color(map[x, y] * color.r, map[x, y] * color.g, map[x, y] * color.b);
            }
        }

        return colorMap;
    }

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

    public static Color[] GenerateForTerrainMap(int dimensions, float[,] map, MapBuilder.TerrainFeature[] features) {
        Color[] colorMap = new Color[dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                foreach (MapBuilder.TerrainFeature feature in features) {
                    if (map[x, y] <= feature.heightThreshold) {
                        colorMap[y * dimensions + x] = feature.colour;
                        break;
                    }
                }
            }
        }

        return colorMap;
    }

    public static Color[] GenerateForTerrainMap(int dimensions, float[,,] map, MapBuilder.TerrainFeature[] features) {
        Color[] colorMap = new Color[dimensions * dimensions * dimensions];
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                for (int z = 0; z < dimensions; z++) {
                    foreach (MapBuilder.TerrainFeature feature in features) {
                        if (map[x, y, z] <= feature.heightThreshold) {
                            colorMap[(y + z * dimensions) * dimensions + x] = feature.colour;
                            break;
                        }
                    }
                }
            }
        }

        return colorMap;
    }
}
