using UnityEngine;

public class Player : MonoBehaviour { 
    public GameObject mouseCollider;
    public GameObject selectedObject;
    public GameObject playerCamera;

    public float cameraSpeed = 1;

    void Update() {
        //Position Mouse Collider
        mouseCollider.transform.position = new Vector3(Input.mousePosition.x, 0, Input.mousePosition.y);
        //Move Camera
        CameraMovement();
    }

    private void CameraMovement() {
        if (Input.GetKey(KeyCode.W)) 
            playerCamera.transform.Translate(cameraSpeed * Time.deltaTime * Vector3.forward, Space.World);        

        if (Input.GetKey(KeyCode.A)) 
            playerCamera.transform.Translate(cameraSpeed * Time.deltaTime * Vector3.left, Space.World);        

        if (Input.GetKey(KeyCode.S)) 
            playerCamera.transform.Translate(cameraSpeed * Time.deltaTime * Vector3.back, Space.World);        

        if (Input.GetKey(KeyCode.D)) 
            playerCamera.transform.Translate(cameraSpeed * Time.deltaTime * Vector3.right, Space.World);        
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (Input.GetMouseButton(0)) {
            selectedObject = collision.gameObject;
        }

        if (Input.GetMouseButton(1)) {
            selectedObject.transform.Translate(mouseCollider.transform.position);
        }
    }
}
