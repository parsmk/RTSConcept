using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceHandler2D {
    ComputeShader resourceHandler2D;

    public void GenerateResources2D(int dimensions, float[] map, TerrainType[] terrainTypes) {
        Vector2[] seedPoints = GenerateWorleySeedPoints(dimensions, map, terrainTypes).ToArray();
        //tempWorley = Worley(seedPoints, noiseData);
    }

    private List<Vector2> GenerateWorleySeedPoints(int dimensions, float[] map, TerrainType[] terrainTypes) {
        List<Vector2> output = new List<Vector2>();
        Dictionary<string, List<Vector2>> rawTerrainTypes = new Dictionary<string, List<Vector2>>();

        //Single Pass Classification
        for (int x = 0; x < dimensions; x++) {
            for (int y = 0; y < dimensions; y++) {
                Vector2 currentPoint = new Vector2(x, y);
                foreach (TerrainType terrainType in terrainTypes) {
                    if (terrainType.minHeight < map[y * dimensions + x] && map[y * dimensions + x] < terrainType.maxHeight) {
                        if (!rawTerrainTypes.TryAdd(terrainType.terrainTypeName, new List<Vector2> { currentPoint })) {
                            rawTerrainTypes[terrainType.terrainTypeName].Add(currentPoint);
                        }
                    }
                }
            }
        }

        //FloodFill and Centroid Calculation
        foreach (List<Vector2> terrainType in rawTerrainTypes.Values) {
            HashSet<Vector2> hashTT = new HashSet<Vector2>(terrainType);
            List<List<Vector2>> landMassesOfTerrainType = FloodFill(hashTT, dimensions);
            foreach (List<Vector2> landMass in landMassesOfTerrainType) {
                output.Add(Centroid(landMass));
            }
        }

        return output;
    }

    private List<List<Vector2>> FloodFill(HashSet<Vector2> terrainData, int dimensions) {
        List<List<Vector2>> output = new List<List<Vector2>>();
        Stack<Vector2> stack = new Stack<Vector2>();
        bool[,] visited = new bool[dimensions, dimensions];

        while (terrainData.Count > 0) {
            List<Vector2> landMass = new List<Vector2>();
            stack.Push(terrainData.First());

            Vector2 currentVertex;
            Vector2[] neighbouringVertices;
            while (stack.Count > 0) {
                currentVertex = stack.Pop();
                visited[(int)currentVertex.x, (int)currentVertex.y] = true;

                //Assuming currentVertex = (0,0)
                neighbouringVertices = new Vector2[8];
                neighbouringVertices[0] = currentVertex + Vector2.up + Vector2.left;
                neighbouringVertices[1] = currentVertex + Vector2.up;
                neighbouringVertices[2] = currentVertex + Vector2.up + Vector2.right;
                neighbouringVertices[3] = currentVertex + Vector2.left;
                neighbouringVertices[4] = currentVertex + Vector2.right;
                neighbouringVertices[5] = currentVertex + Vector2.down + Vector2.left;
                neighbouringVertices[6] = currentVertex + Vector2.down;
                neighbouringVertices[7] = currentVertex + Vector2.down + Vector2.right;

                foreach (Vector2 neighbour in neighbouringVertices) {
                    CheckAndPushVertex(neighbour, stack, visited, terrainData, dimensions);
                }

                landMass.Add(currentVertex); terrainData.Remove(currentVertex);
            }

            output.Add(landMass);
        }

        return output;
    }

    private void CheckAndPushVertex(Vector2 vertex, Stack<Vector2> stack, bool[,] visited, HashSet<Vector2> terrainData, int dimensions) {
        if (vertex.x < 0 || vertex.x >= dimensions || vertex.y < 0 || vertex.y >= dimensions)
            return;
        if (visited[(int)vertex.x, (int)vertex.y])
            return;
        if (!terrainData.Contains(vertex))
            return;

        stack.Push(vertex);
    }

    private Vector2 Centroid(List<Vector2> coordinates) {
        Vector2 output;
        float x = 0f, y = 0f;

        foreach (Vector2 vector in coordinates) {
            x += vector.x;
            y += vector.y;
        }

        x /= coordinates.Count;
        y /= coordinates.Count;

        output = new Vector2(x, y);

        return output;
    }
}