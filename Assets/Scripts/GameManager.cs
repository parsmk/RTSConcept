using UnityEngine;

public class GameManager : MonoBehaviour {
    private static Resource[] resources;

    public Resource[] GetResources() {
        if (resources == null) {
            JsonUtility.FromJson(Resources.Load<Resource>("Data/resources"));
        }

        return resources;
    }
}