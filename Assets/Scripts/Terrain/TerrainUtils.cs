using UnityEngine;

public static class TerrainUtils { 
    public static int Index2D(int x, int y, int width) {
        return y * width + x;
    }

    public static int Index2D(Vector2 vertex, int width) {
        return Index2D((int)vertex.x, (int)vertex.y, width);
    }
}