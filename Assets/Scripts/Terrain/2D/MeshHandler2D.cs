using UnityEngine;

public class MeshHandler2D : MonoBehaviour {

    public struct MeshData2D {
        public Vector3[] vertexArray;
        public Vector2[] uvRays;
        public int[] triangleArray;
        
        public MeshData2D(Vector3[] vertexArray, Vector2[] uvRays, int[] triangleArray) {
            this.vertexArray = vertexArray;
            this.uvRays = uvRays;
            this.triangleArray = triangleArray;
        }
    }

    private struct MeshBuffer {
        public static int size = sizeof(int) * 2;
        public int dimensions;
        public int mapHeight;

        public MeshBuffer(int dimensions, int mapHeight) {
            this.dimensions = dimensions;
            this.mapHeight = mapHeight;
        }
    }

    public ComputeShader computeMesh2D;
    public int heightModifier = 0;

    public Mesh GenerateMesh(int dimensions, int mapHeight, float[] map) {
        // Prepare Buffer Data
        MeshBuffer meshData = new(dimensions, mapHeight);

        int kernelID = computeMesh2D.FindKernel("ComputeMesh");
        int[] triangleArray = new int[(dimensions - 1) * (dimensions - 1) * 6];
        Vector3[] vertexArray = new Vector3[dimensions * dimensions];
        Vector2[] uvRays = new Vector2[dimensions * dimensions];

        // Create Buffers
        using (BufferManager bf = new(computeMesh2D, kernelID)) {
            bf.PrepareConstantBuffer(meshData, MeshBuffer.size, "meshData");
            bf.PrepareBuffer(map, map.Length, sizeof(float), "map");

            ComputeBuffer triangleBuffer = bf.PrepareOutputBuffer(triangleArray.Length, sizeof(int), "triangles");
            ComputeBuffer vertexBuffer = bf.PrepareOutputBuffer(vertexArray.Length, sizeof(float) * 3, "vertexArray");
            ComputeBuffer uvBuffer = bf.PrepareOutputBuffer(uvRays.Length, sizeof(float) * 2, "uvRays");

            int packets = dimensions / 8;
            computeMesh2D.Dispatch(kernelID, packets, packets, 1);

            triangleBuffer.GetData(triangleArray);
            vertexBuffer.GetData(vertexArray);
            uvBuffer.GetData(uvRays);
        }

        return CreateMesh(vertexArray, uvRays, triangleArray);
    }

    private Mesh CreateMesh(Vector3[] vertexArray, Vector2[] uvRays, int[] triangleArray) {
        Mesh mesh = new Mesh();
        mesh.vertices = vertexArray;
        mesh.uv = uvRays;
        mesh.triangles = triangleArray;
        mesh.RecalculateNormals();
        return mesh;
    }

    private void OnValidate() {
        if (heightModifier < 0) { heightModifier = 0; }
    }
}