using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayController : MonoBehaviour
{
    public GameObject sun;
    public GameObject moon;
    public GameObject mapGen;

    public float angleOffset = 90;
    public float offsetTime = 0;

    private float radius;
    private float angle = 90;
    private Vector3 currentSunRotation = new Vector3(0, 90, 0);
    private Vector3 currentMoonRotation = new Vector3(0, 270, 0);

    void Start() {
        MapBuilder mapBuilder = mapGen.GetComponent<MapBuilder>();
        radius = (mapBuilder.mapRegionDimensions * MapBuilder.regionSize) / 2;

        currentMoonRotation.x = angle + 180 + angleOffset;
        currentSunRotation.x = angle - angleOffset;

        InvokeRepeating("CalculateOrbit", 0, offsetTime);
    }

    void CalculateOrbit() {
        float angleInRadians = angle * (Mathf.PI / 180);

        if (sun.transform.position.y < 0) {
            sun.SetActive(false);
            moon.SetActive(true);
        }
        if (moon.transform.position.y < 0) {
            sun.SetActive(true);
            moon.SetActive(false);
        }

        float sunX = radius * Mathf.Cos(angleInRadians) + radius;
        float sunY = radius * Mathf.Sin(angleInRadians);
        sun.transform.position = new Vector3(sunX, sunY, 0);
        currentSunRotation.x += angleOffset; sun.transform.eulerAngles = currentSunRotation;

        float moonX = radius * Mathf.Cos(angleInRadians + Mathf.PI) + radius;
        float moonY = radius * Mathf.Sin(angleInRadians + Mathf.PI);
        moon.transform.position = new Vector3(moonX, moonY, 0);
        currentMoonRotation.x -= angleOffset; moon.transform.eulerAngles = currentMoonRotation;

        angle -= angleOffset;
    }
}
//print("Radius: " + radius + "Angle (Degrees, Radians) = (" + angle + ", " + angleInRadians + ")");