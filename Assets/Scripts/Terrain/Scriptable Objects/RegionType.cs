using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

[System.Serializable]
public class RegionType : ScriptableObject {
    public string regionTypeName;
    public TerrainType[] terrainTypes;

    public static RegionType CreateInstance(string name, TerrainType[] terrainTypes) {
        RegionType newInstance = ScriptableObject.CreateInstance<RegionType>();
        newInstance.regionTypeName = name;
        newInstance.terrainTypes = terrainTypes;

        return newInstance;
    }

    public static RegionType LoadRegionType(string name, XDocument source, TerrainType[] terrainTypes) {
        //Query for RegionType by regionTypeName
        IEnumerable<XElement> regionTypeQuery = from regionType in source.Root.Descendants("regionType")
                                                where regionType.Attribute("name").Value.Equals(name)
                                                select regionType;

        XElement xmlRegionType = regionTypeQuery.FirstOrDefault();

        return CreateInstance(name, terrainTypes);
    }
}