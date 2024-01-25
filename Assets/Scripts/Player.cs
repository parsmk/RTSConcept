using UnityEngine;

public class Player : MonoBehaviour { 
    public GameObject selectedObject;
    public GameObject playerCamera;

    public float cameraSpeed = 1;
    public float dragSpeed = 1;
    public Vector3 dragOrigin;

    private void Start() {
        Cursor.lockState = CursorLockMode.Confined;
    }

    #region Update Functions
    void Update() {
        CameraMovement();
    }

    private void FixedUpdate() {
        if (Input.GetMouseButtonDown(0)) {
            SelectObject();
        }
    }
    #endregion

    #region Movement Functions
    private void CameraMovement() {
        ButtonMovement();
        DragMovement();
        MouseMovement();
    }

    private void ButtonMovement() {
        if (Input.GetKey(KeyCode.W))
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.forward, Space.World);

        if (Input.GetKey(KeyCode.A))
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.left, Space.World);

        if (Input.GetKey(KeyCode.S))
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.back, Space.World);

        if (Input.GetKey(KeyCode.D))
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.right, Space.World);
    }

    private void DragMovement() {
        if (Input.GetMouseButtonDown(0))
            dragOrigin = Input.mousePosition;

        if (!Input.GetMouseButton(0))
            return;

        Vector3 movement = playerCamera.GetComponent<Camera>().ScreenToViewportPoint(dragOrigin - Input.mousePosition);
        Vector3 newPosition = new Vector3(movement.x * dragSpeed, 0, movement.y * dragSpeed);

        transform.Translate(newPosition, Space.World);
    }

    private void MouseMovement() {
        if (Input.GetMouseButton(0))
            return;

        Vector3 mousePos = playerCamera.GetComponent<Camera>().ScreenToViewportPoint(Input.mousePosition);

        if (mousePos.y > 0.9)
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.forward, Space.World);

        if (mousePos.x < 0.1)
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.left, Space.World);

        if (mousePos.y < 0.1)
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.back, Space.World);

        if (mousePos.x > 0.9)
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.right, Space.World);
    }
    #endregion

    private void SelectObject() {
        RaycastHit hit;
        Ray ray = playerCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit) && hit.transform.CompareTag("InteractableObject")) {
            selectedObject = hit.transform.gameObject;
        }

        Debug.DrawRay(ray.origin, ray.direction * 500);
    }
}
