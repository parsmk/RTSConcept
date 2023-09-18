using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    public float cameraSpeed = 1;

    void Update()
    {
        if (Input.GetKey(KeyCode.W)) {
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.forward, Space.World);
        }

        if (Input.GetKey(KeyCode.A)) {
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.left, Space.World);
        }

        if (Input.GetKey(KeyCode.S)) {
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.back, Space.World);
        }

        if (Input.GetKey(KeyCode.D)) {
            transform.Translate(cameraSpeed * Time.deltaTime * Vector3.right, Space.World);
        }
    }
}
