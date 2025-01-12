using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

[System.Serializable]
public class TerrainType : ScriptableObject{
    public string terrainTypeName;
    public float maxHeight;
    public float minHeight;
    public Color color;
    
    public static TerrainType CreateInstance(string name, float maxHeight, float minHeight, Color color) {
        TerrainType newInstance = ScriptableObject.CreateInstance<TerrainType>();
        newInstance.terrainTypeName = name;
        newInstance.maxHeight = maxHeight;
        newInstance.minHeight = minHeight;
        newInstance.color = color;

        return newInstance;
    }

    public static TerrainType[] LoadTerrainTypes(string name, XDocument source) {
        //Query for TerrainTypes by regionTypeName
        IEnumerable<XElement> terrainTypeQuery = from regionType in source.Root.Descendants("regionType")
                                                 where regionType.Attribute("name").Value.Equals(name)
                                                 from terrainType in regionType.Element("terrainTypes").Elements("terrainType")
                                                 select terrainType;

        List<XElement> xmlTerrainTypes = terrainTypeQuery.ToList();
        TerrainType[] terrainTypes = new TerrainType[xmlTerrainTypes.Count];

        for (int i = 0; i < xmlTerrainTypes.Count; i++) {
            XElement xmlColor = xmlTerrainTypes[i].Element("color");
            Color color = new Color(float.Parse(xmlColor.Attribute("r").Value) / 255f,
                                    float.Parse(xmlColor.Attribute("g").Value) / 255f,
                                    float.Parse(xmlColor.Attribute("b").Value) / 255f);

            TerrainType newTerrainType = CreateInstance(xmlTerrainTypes[i].Attribute("name").Value,
                                              float.Parse(xmlTerrainTypes[i].Element("maxHeight").Value),
                                              float.Parse(xmlTerrainTypes[i].Element("minHeight").Value),
                                              color);

            terrainTypes[i] = newTerrainType;
        }

        return terrainTypes;
    }
}