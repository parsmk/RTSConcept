using UnityEngine;

public class Region {
    public GameObject regionObject;
    public Mesh mesh;
    public Texture texture;

    public RegionType regionType;
    public string regionName;


    public Region(GameObject regionObject, RegionType regionType, Mesh mesh, Texture texture) {
        this.regionObject = regionObject;
        this.regionType = regionType;
        this.mesh = mesh;
        this.texture = texture;
    }
}