using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceHandler2D : MonoBehaviour {
    public class LandMass2D {
        public TerrainType terrainType;
        public Vector2 minX, maxX;
        public Vector2 minY, maxY;
        public Vector2[] vertices;
        public Vector2 seedPoint;

        public LandMass2D(TerrainType terrainType, Vector2 minX, Vector2 maxX, Vector2 minY, Vector2 maxY, Vector2[] vertices) {
            this.terrainType = terrainType;
            this.minX = minX; this.maxX = maxX;
            this.minY = minY; this.maxY = maxY;
            this.vertices = vertices;
        }

        public float GetExtent() {
            float width = maxX.x - minX.x;
            float height = maxY.x - minY.x;

            return width * width + height * height;
        }
    }

    private static readonly Vector2[] neighborOffsets = {
        new(-1, 1) , new(0, 1) , new(1, 1),
        new(-1, 0) ,             new(1, 0),
        new(-1, -1), new(0, -1), new(1, -1)
    };

    public ComputeShader computeResource2D;

    //Temp
    public List<LandMass2D> _landMasses;
    public List<Vector2> seedPoints = new();
    //

    public List<LandMass2D> GenerateLandMasses(int dimensions, Vector2 offset, float[] map, TerrainType[] terrainTypes) {
        //Single Pass Classification
        Dictionary<TerrainType, List<Vector2>> verticesPerTerrainType = SinglePassClassification(terrainTypes, map, dimensions);

        //Extract LandMass and compute Max/Mins
        List<LandMass2D> landMasses = new();
        float[] mapDensity = new float[dimensions * dimensions];
        float maxArea = float.MinValue, maxExtent = float.MinValue;

        //Check how to expand by tuple
        foreach (KeyValuePair<TerrainType, List<Vector2>> terrainType in verticesPerTerrainType) {
            HashSet<Vector2> hashTT = new(terrainType.Value);
            List<LandMass2D> landMassesOfTerrainType = ExtractLandMasses(terrainType.Key, hashTT, mapDensity, dimensions);

            foreach (LandMass2D landMass in landMassesOfTerrainType) {
                landMasses.Add(landMass);
                maxArea = Mathf.Max(landMass.vertices.Length, maxArea);
                maxExtent = Mathf.Max(landMass.GetExtent(), maxExtent);
            }
        }

        //Compute points
        foreach(LandMass2D landMass in landMasses) {
            _landMasses.Add(landMass);
        }

        return landMasses;
    }

    private Dictionary<TerrainType, List<Vector2>> SinglePassClassification(TerrainType[] terrainTypes, float[] map, int dimensions) {
        Dictionary<TerrainType, List<Vector2>> output = new();

        for (int x = 0; x < dimensions; x++) {
            for (int y = 0; y < dimensions; y++) {
                Vector2 currentVertex = new(x, y);
                int currentIndex = TerrainUtils.Index2D(x, y, dimensions);

                foreach (TerrainType terrainType in terrainTypes) {
                    if (terrainType.minHeight < map[currentIndex] && map[currentIndex] < terrainType.maxHeight) {
                        if (!output.TryAdd(terrainType, new List<Vector2> { currentVertex }))
                            output[terrainType].Add(currentVertex);
                    }
                }
            }
        }

        return output;
    }

    private List<LandMass2D> ExtractLandMasses(TerrainType terrainType, HashSet<Vector2> terrainData, float[] density, int dimensions) {
        List<LandMass2D> output = new();
        Stack<Vector2> stack = new();
        bool[] visited = new bool[dimensions * dimensions];

        while (terrainData.Count > 0) {
            byte[] neighbors = new byte[dimensions * dimensions];
            Vector2 minX, maxX, minY, maxY;
            List<Vector2> vertices = new();
            minX = minY = new Vector2(dimensions, dimensions);
            maxX = maxY = new Vector2(0, 0);

            stack.Push(terrainData.First());

            while (stack.Count > 0) {
                Vector2 current = stack.Pop();
                int index = TerrainUtils.Index2D(current, dimensions);

                byte neighborMask = ComputeNeighbors(current, terrainData, dimensions);
                neighbors[index] = neighborMask;

                visited[index] = true;

                for (int i = 0; i < neighborOffsets.Length; i++) { 
                    int currentBit = neighborMask & (1 << i);
                    if (currentBit == 0) continue;

                    Vector2 neighbor = current + neighborOffsets[i];

                    if (visited[TerrainUtils.Index2D(neighbor, dimensions)])
                        continue;

                    density[index]++;

                    stack.Push(neighbor);
                }

                vertices.Add(current); terrainData.Remove(current);

                minX = (current.x < minX.x) ? new Vector2(current.x, current.y) : minX;
                minY = (current.y < minY.y) ? new Vector2(current.x, current.y) : minY;
                maxX = (current.x > maxX.x) ? new Vector2(current.x, current.y) : maxX;
                maxY = (current.y > maxY.y) ? new Vector2(current.x, current.y) : maxY;
            }

            LandMass2D landMass = new(terrainType, maxY, minX, maxX, minY, vertices.ToArray());
            landMass.seedPoint = CalculateLandMassDensity(landMass.vertices, neighbors, density, dimensions);
            //landMass.seedPoint = Centroid(landMass.vertices);

            output.Add(landMass);
        }

        return output;
    }

    private Vector2 CalculateLandMassDensity(Vector2[] vertices, byte[] neighbors, float[] density, int dimensions) {
        Queue<(Vector2, float)> queue = new(); 

        (Vector2 vertex, float density) maxDensity = (Vector2.zero, float.MinValue);
        float decayFactor = 0.8f;

        foreach (Vector2 vertex in vertices) { queue.Enqueue((vertex, density[TerrainUtils.Index2D(vertex, dimensions)])); }

        while (queue.Count > 0) {
            (Vector2 vertex, float decayedDensity) current  = queue.Dequeue();

            if (current.decayedDensity < 1) continue;

            int currentIndex = TerrainUtils.Index2D(current.vertex, dimensions);
            float propagatedDensity = current.decayedDensity * decayFactor;

            for (int i = 0; i < neighborOffsets.Length; i++) {
                int currentBit = neighbors[currentIndex] & (1 << i);
                if (currentBit == 0) continue;

                Vector2 neighbor = current.vertex + neighborOffsets[i];
                int neighborIndex = TerrainUtils.Index2D(neighbor, dimensions);

                density[neighborIndex] += propagatedDensity;
                maxDensity = (density[neighborIndex] > maxDensity.density) ? (neighbor, density[neighborIndex]) : maxDensity;
                queue.Enqueue((neighbor, propagatedDensity));
            }
        }

        return maxDensity.vertex;
    }

    private byte ComputeNeighbors(Vector2 current, HashSet<Vector2> set, int dimensions) {
        byte output = 0;

        for (int i = 0; i < neighborOffsets.Length; i++) {
            Vector2 proposed = current + neighborOffsets[i];

            if (proposed.x < 0 || proposed.x >= dimensions || proposed.y < 0 || proposed.y >= dimensions)
                continue;
            if (!set.Contains(proposed))
                continue;

            output |= (byte)(1 << i);
        }

        return output;
    }

    private float ScoreFunction(LandMass2D landMass, float maxArea, float maxExtent) {
        float area = landMass.vertices.Length / maxArea;
        float extent = landMass.GetExtent() / maxExtent;

        return 0.65f * area + 0.35f * extent;
    }

    private Vector2 Centroid(Vector2[] coordinates) {
        Vector2 output;
        float x = 0f, y = 0f;

        foreach (Vector2 vector in coordinates) {
            x += vector.x;
            y += vector.y;
        }

        x /= coordinates.Length;
        y /= coordinates.Length;

        output = new Vector2(x, y);

        return output;
    }

    public void OnDrawGizmos() {
        if (_landMasses == null || _landMasses.Count < 1) return;
        foreach (LandMass2D landMass in _landMasses) {
            Gizmos.color = landMass.terrainType.color * 2;

            if (landMass.seedPoint == null)
                continue;


            Gizmos.DrawSphere(new Vector3(landMass.maxX.x, 50, landMass.maxX.y), 2);
            Gizmos.DrawSphere(new Vector3(landMass.maxY.x, 50, landMass.maxY.y), 2);
            Gizmos.DrawSphere(new Vector3(landMass.minX.x, 50, landMass.minX.y), 2);
            Gizmos.DrawSphere(new Vector3(landMass.minY.x, 50, landMass.minY.y), 2);
            //Gizmos.DrawSphere(new Vector3(landMass.seedPoint.x, 50, landMass.seedPoint.y), 2);
        }
    }
}