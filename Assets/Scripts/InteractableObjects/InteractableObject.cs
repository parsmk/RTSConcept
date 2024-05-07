using UnityEngine;

public class InteractableObject : MonoBehaviour {
    public ObjectStats objectStats;
    private Vector3 prevPosition;

    //Mesh
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public Bounds meshBounds;

    private void Start() {
        //GetComponents
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        transform.tag = "InteractableObject";
        meshBounds = meshRenderer.bounds;

        prevPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
    }

    private void Update() {
        Move();

        // Keep unit on terrain
        if (prevPosition.x != transform.position.x || prevPosition.z != transform.position.z) {
            RaycastHit hit;
            LayerMask terrainMask = LayerMask.GetMask("Terrain");
            if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, terrainMask)) {
                transform.position = hit.point + meshBounds.extents;
            }
        }

        prevPosition = transform.localPosition;
    }

    public void Move() {
        if (Input.GetMouseButtonDown(1)) {
            // Move unit to location
        }
    }

    public bool PerformAction() {
        return true;
    }
}
