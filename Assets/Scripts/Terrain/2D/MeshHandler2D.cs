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
        MeshBuffer meshData = new MeshBuffer(dimensions, mapHeight);

        // Create Buffers
        ComputeBuffer triangleBuffer = new ComputeBuffer((dimensions - 1) * (dimensions - 1) * 6, sizeof(int));
        ComputeBuffer vertexBuffer = new ComputeBuffer(dimensions * dimensions, sizeof(float) * 3);
        ComputeBuffer uvBuffer = new ComputeBuffer(dimensions * dimensions, sizeof(float) * 2);
        ComputeBuffer mapBuffer = new ComputeBuffer(dimensions * dimensions, sizeof(float));
        ComputeBuffer meshBuffer = new ComputeBuffer(1, MeshBuffer.size, ComputeBufferType.Constant);

        // Set Buffer Data
        mapBuffer.SetData(map);
        meshBuffer.SetData(new[] { meshData });

        // Prepare Buffers for Dispatch
        int kernelID = computeMesh2D.FindKernel("ComputeMesh");
        computeMesh2D.SetBuffer(kernelID, "triangles", triangleBuffer);
        computeMesh2D.SetBuffer(kernelID, "vertexArray", vertexBuffer);
        computeMesh2D.SetBuffer(kernelID, "uvRays", uvBuffer);
        computeMesh2D.SetBuffer(kernelID, "map", mapBuffer);
        computeMesh2D.SetConstantBuffer("meshData", meshBuffer, 0, MeshBuffer.size);

        // Dispatch
        int packets = dimensions / 8;
        computeMesh2D.Dispatch(kernelID, packets, packets, 1);

        // Retrieve Data
        int[] triangleArray = new int[(dimensions - 1) * (dimensions - 1) * 6];
        Vector3[] vertexArray = new Vector3[dimensions * dimensions];
        Vector2[] uvRays = new Vector2[dimensions * dimensions];
        triangleBuffer.GetData(triangleArray);
        vertexBuffer.GetData(vertexArray);
        uvBuffer.GetData(uvRays);

        // Release Resources
        triangleBuffer.Release();
        vertexBuffer.Release();
        uvBuffer.Release();
        mapBuffer.Release();
        meshBuffer.Release();

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