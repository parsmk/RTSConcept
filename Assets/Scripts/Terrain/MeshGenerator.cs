using UnityEngine;

public static class MeshGenerator {
    //2D Space
    public static MeshData GenerateMeshData(float[,] map, int dimensions, float heightModifier, AnimationCurve animationCurve) {
        MeshData meshData = new MeshData(dimensions, dimensions);

        for (int y = 0, vertexIndex = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++, vertexIndex++) {
                meshData.vertexArray[vertexIndex] = new Vector3(x, animationCurve.Evaluate(map[x, y]) * heightModifier, y);
                meshData.uvRays[vertexIndex] = new Vector2(x / (float)dimensions, y / (float)dimensions);

                if (x < dimensions - 1 && y < dimensions - 1) {
                    meshData.AddTriangle(vertexIndex + dimensions, vertexIndex + 1, vertexIndex);
                    meshData.AddTriangle(vertexIndex + dimensions, vertexIndex + dimensions + 1, vertexIndex + 1);
                }
            }
        }

        return meshData;
    }

    //3D Space
    public static MeshData GenerateMeshData(float[,,] map, int dimensions, float threshhold, float textureDetail, int meshDetail) {
        //Marching Cubes Algorithm

        MeshData meshData = new MeshData(dimensions, dimensions, dimensions);
        Vector3[] outputVertices = new Vector3[dimensions * dimensions * dimensions];
        Vector2[] uvRays = new Vector2[dimensions * dimensions * dimensions];

        int vertIndex = 0;
        int edgeSize = (dimensions - 1) / meshDetail;

        // Go through the space
        for (int x = 0; x < dimensions - edgeSize; x += edgeSize) {
            for (int y = 0; y < dimensions - edgeSize; y += edgeSize) {
                for (int z = 0; z < dimensions - edgeSize; z += edgeSize) {
                    (Vector3 cord, float value)[] cubeVertices = new (Vector3 cord, float value)[8];
                    int cubeIndex = 0;

                    //Choose current Cube vertex
                    cubeVertices[0] = (new Vector3(x, y, z), 
                                               map[x, y, z]);
                    cubeVertices[1] = (new Vector3(x + edgeSize, y, z), 
                                               map[x + edgeSize, y, z]);
                    cubeVertices[2] = (new Vector3(x, y + edgeSize, z), 
                                               map[x, y + edgeSize, z]);
                    cubeVertices[3] = (new Vector3(x, y, z + edgeSize), 
                                               map[x, y, z + edgeSize]);
                    cubeVertices[4] = (new Vector3(x + edgeSize, y, z + edgeSize), 
                                               map[x + edgeSize, y, z + edgeSize]);
                    cubeVertices[5] = (new Vector3(x + edgeSize, y + edgeSize, z), 
                                               map[x + edgeSize, y + edgeSize, z]);
                    cubeVertices[6] = (new Vector3(x, y + edgeSize, z + edgeSize), 
                                               map[x, y + edgeSize, z + edgeSize]);
                    cubeVertices[7] = (new Vector3(x + edgeSize, y + edgeSize, z + edgeSize), 
                                               map[x + edgeSize, y + edgeSize, z + edgeSize]);

                    // Check which vertices are inside model
                    for (int i = 0; i < cubeVertices.Length; i++) {
                        if (cubeVertices[i].value < threshhold) {
                            cubeIndex |= 1 << i;
                        }
                    }

                    // Find edge amongst possiblities
                    int edgesIntersected = MarchingCubesConstants.edgeTable[cubeIndex];
                    Vector3[] newVertices = new Vector3[12];

                    // if no edges are intersecting don't need to do anything
                    if (edgesIntersected == 0)
                        continue;

                    // otheriwise 
                    for (int edge = 1, i = 0; edge <= 0xfff; edge <<= 1, i++) {
                        if ((edgesIntersected & edge) != 0) {
                            // Choose corners based on edgeTable
                            // GetMapValue
                            float valueVertA = cubeVertices[MarchingCubesConstants.edgeIndices[i].a].value;
                            float valueVertB = cubeVertices[MarchingCubesConstants.edgeIndices[i].b].value;

                            // GetPosition
                            Vector3 cordVertA = cubeVertices[MarchingCubesConstants.edgeIndices[i].a].cord;
                            Vector3 cordVertB = cubeVertices[MarchingCubesConstants.edgeIndices[i].b].cord;

                            // Interpolation Factor
                            float factor = (threshhold - valueVertA) / (valueVertB - valueVertA);

                            // Interpolate
                            float outputX = cordVertA.x + factor * (cordVertB.x - cordVertA.x);
                            float outputY = cordVertA.y + factor * (cordVertB.y - cordVertA.y);
                            float outputZ = cordVertA.z + factor * (cordVertB.z - cordVertA.z);

                            // Store newVertex
                            newVertices[i] = new Vector3(outputX, outputY, outputZ);
                        }
                    }

                    for (int i = 0; MarchingCubesConstants.triangulationTable[cubeIndex][i] != -1; i += 3) {
                        // Get Model Triangles
                        int triA = MarchingCubesConstants.triangulationTable[cubeIndex][i];
                        int triB = MarchingCubesConstants.triangulationTable[cubeIndex][i + 1];
                        int triC = MarchingCubesConstants.triangulationTable[cubeIndex][i + 2];

                        // Calculate UVVertex
                        Vector2 uv = new Vector2(outputVertices[vertIndex].x / dimensions, outputVertices[vertIndex].z / dimensions) * textureDetail;

                        // newVertex Index
                        int vertIndexA = vertIndex;
                        int vertIndexB = vertIndex + 1;
                        int vertIndexC = vertIndex + 2;

                        // Assign newVertices in order of the triangle to Mesh 
                        outputVertices[vertIndexA] = newVertices[triA];
                        outputVertices[vertIndexB] = newVertices[triB];
                        outputVertices[vertIndexC] = newVertices[triC];

                        // Assign UV for each vertex
                        uvRays[vertIndexA] = CalculateCubicUV(newVertices[triA]);
                        uvRays[vertIndexB] = CalculateCubicUV(newVertices[triB]);
                        uvRays[vertIndexC] = CalculateCubicUV(newVertices[triC]);

                        // Add Triangle to MeshData
                        meshData.AddTriangle(vertIndexA, vertIndexB, vertIndexC);

                        vertIndex += 3;
                    }

                }
            }
        }

        // Populate VertexARray
        meshData.vertexArray = outputVertices;
        meshData.uvRays = uvRays;

        return meshData;
    }

    private static Vector2 CalculateCubicUV(Vector3 currentVertex) {
        Vector2 output = new Vector2();

        float maxDimension = Mathf.Max(Mathf.Max(currentVertex.x, currentVertex.y), currentVertex.z);

        if (maxDimension == currentVertex.x) {
            output = new Vector2(currentVertex.y, currentVertex.z);
        } else if (maxDimension == currentVertex.y) {
            output = new Vector2(currentVertex.x, currentVertex.z);
        } else {
            return new Vector2(currentVertex.x, currentVertex.y);
        }

        return output;
    }

    public struct MeshData {
        public Vector3[] vertexArray;
        public Vector2[] uvRays;
        public int[] triangleArray;
        public int triangleIndex;

        public MeshData(int meshWidth, int meshHeight) {
            vertexArray = new Vector3[meshWidth * meshHeight];
            uvRays = new Vector2[meshWidth * meshHeight];
            triangleArray = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
            triangleIndex = 0;
        }

        public MeshData(int meshWidth, int meshHeight, int meshDepth) {
            vertexArray = new Vector3[meshWidth * meshHeight * meshDepth];
            uvRays = new Vector2[meshWidth * meshHeight * meshDepth];
            triangleArray = new int[(meshWidth - 1) * (meshHeight - 1) * (meshDepth - 1) * 6];
            triangleIndex = 0;
        }

        public void AddTriangle(int a, int b, int c) {
            triangleArray[triangleIndex] = a;
            triangleArray[triangleIndex + 1] = b;
            triangleArray[triangleIndex + 2] = c;

            triangleIndex += 3;
        }

        public Mesh CreateMesh() {
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.vertices = vertexArray;
            mesh.triangles = triangleArray;
            mesh.uv = uvRays;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
