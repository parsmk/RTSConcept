using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using UnityEngine;
using static NoiseGenerator;
using static MeshGenerator;
using static ColorGenerator;

public class MapBuilder : MonoBehaviour {
    #region Structs and Enums
    public struct MapData2D {
        public int dimensions;
        public NoiseData2D noiseData;
        public MeshData meshData;
        public Color[] colorMap;

        public MapData2D(int dimensions, NoiseData2D noiseData, MeshData meshData, Color[] colorMap) {
            this.dimensions = dimensions;
            this.noiseData = noiseData;
            this.meshData = meshData;
            this.colorMap = colorMap;
        }

        public static implicit operator MapData2D(MapData3D mapData2D) {
            return new MapData2D(mapData2D.dimensions, mapData2D.noiseData, mapData2D.meshData, mapData2D.colorMap);
        }
    }

    public struct MapData3D {
        public int dimensions;
        public NoiseData3D noiseData;
        public MeshData meshData;
        public Color[] colorMap;

        public MapData3D(int dimensions, NoiseData3D noiseData, MeshData meshData, Color[] colorMap) {
            this.dimensions = dimensions;
            this.noiseData = noiseData;
            this.meshData = meshData;
            this.colorMap = colorMap;
        }

        public static implicit operator MapData3D(MapData2D mapData2D) {
            return new MapData3D(mapData2D.dimensions, mapData2D.noiseData, mapData2D.meshData, mapData2D.colorMap);
        }
    }

    [System.Serializable]
    public struct TerrainType {
        public string name;
        public float maxHeight;
        public float minHeight;
        public Color color;

        public TerrainType(string name, float maxHeight, float minHeight, Color color) {
            this.name = name;
            this.maxHeight = maxHeight;
            this.minHeight = minHeight;
            this.color = color;
        } 
    }

    [System.Serializable]
    public struct RegionType {
        public string name;
        public TerrainType[] terrainTypes;

        public RegionType(string name, TerrainType[] terrainTypes) {
            this.name = name;
            this.terrainTypes = terrainTypes;
        }
    }

    public enum MapType { NoiseVisualizor, HeightMap, ComplexMap };
    #endregion

    #region MapSettings
    public GameObject mapWrapper;
    public MapType mapType = MapType.HeightMap;
    public int mapDimensions = 2;
    //[HideInInspector]
    public int mapDepth = 1;
    public static int regionSize = noiseDimensions - 1;
    #endregion

    #region NoiseSettings
    [Header("Noise Settings")]
    public NoiseMode noiseMode;
    public NoiseInterpolateMode noiseInterpolateMode;
    public NoiseLocalInterpolateMode noiseLocalInterpolateMode;

    public int seed = 1;
    public float scale = 20;
    public const int noiseDimensions = 241;
    public Vector3 mapOffset = Vector3.zero;

    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int octaves = 4;
    #endregion

    #region TextureSettings
    [Header("Texture Settings")]
    public DrawMode drawMode;
    //[HideInInspector]
    public Color color;
    public float textureDetail = 2;
    #endregion

    #region MeshSettings
    [Header("Mesh Settings")]
    public int meshDetail = 5;
    [Range(0, 1)]
    public float threshhold = 0.5f;
    public int heightModifier = 50;

    //[HideInInspector]
    public AnimationCurve animationCurve;
    public Material meshMaterial;
    #endregion

    #region Visualizor
    [Header("Visualizor")]
    public GameObject visualizor;
    public Material visualizorMaterial;
    #endregion

    //Will probably get rid of this and actually return to game manager
    private Dictionary<Vector3, Region> regions = new Dictionary<Vector3, Region>(); 

    public void BuildMap() {
        switch(mapType) {
            case MapType.NoiseVisualizor:
                VisualizeNoise(visualizor);
                break;
            case MapType.HeightMap:
                BuildRegions2D();
                break;
            case MapType.ComplexMap:
                BuildRegions3D();
                break;
        }
    }

    public void ClearMap() {
        if (regions.Count < 1)
            return;

        foreach (Vector2 key in regions.Keys)
            DestroyImmediate(regions[key].regionObject);

        regions.Clear();
    }

    public void VisualizeNoise(GameObject obj) {
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();

        NoiseData2D noise = GenerateNoise(seed,
                                          scale,
                                          noiseDimensions,
                                          mapOffset,
                                          lacunarity,
                                          persistence,
                                          octaves,
                                          noiseMode,
                                          noiseInterpolateMode,
                                          noiseLocalInterpolateMode
        );

        Texture2D output = new Texture2D(noiseDimensions, noiseDimensions);
        output.filterMode = FilterMode.Point;
        output.wrapMode = TextureWrapMode.Clamp;
        output.SetPixels(GenerateForColorMap(noiseDimensions, noise.map, color));
        output.Apply();

        renderer.sharedMaterial = visualizorMaterial;
        renderer.sharedMaterial.mainTexture = output;
    }

    private void BuildRegions2D() {
        float maxMapHeight = 0;
        float minMapHeight = 0;
        (Vector3 key, NoiseData2D noise)[] noiseData = new (Vector3 key, NoiseData2D noise)[mapDimensions * mapDimensions];

        for (int x = 0, index = 0; x < mapDimensions; x++) {
            for (int y = 0; y < mapDimensions; y++, index++) {
                Vector3 coord = new Vector3(x, 0, y) * regionSize;

                noiseData[index].key = coord;
                noiseData[index].noise = GenerateNoise(seed,
                                                       scale,
                                                       noiseDimensions,
                                                       new Vector2(coord.x + mapOffset.x, coord.z + mapOffset.y),
                                                       lacunarity,
                                                       persistence,
                                                       octaves,
                                                       noiseMode,
                                                       noiseInterpolateMode,
                                                       noiseLocalInterpolateMode
                );


                if (maxMapHeight < noiseData[index].noise.localMax)
                    maxMapHeight = noiseData[index].noise.localMax;
                if (minMapHeight > noiseData[index].noise.localMin)
                    minMapHeight = noiseData[index].noise.localMin;
            }
        }

        // Check if optimizing can be done using recursion
        for (int i = 0; i < noiseData.Length; i++)
            noiseData[i].noise = NormalizeNoise(noiseData[i].noise, minMapHeight, maxMapHeight);

        foreach ((Vector3 key, NoiseData2D noise) entry in noiseData) {
            MeshData meshData = GenerateMeshData(entry.noise.map, noiseDimensions, heightModifier, animationCurve);
            Color[] colorMap = ChooseColorMap(entry.noise.map, new RegionType());
            MapData2D mapData = new MapData2D(noiseDimensions, entry.noise, meshData, colorMap);

            regions.Add(entry.key, new Region(mapData, entry.key, mapWrapper.transform, meshMaterial));
        }
    }

    private void BuildRegions3D() {
        float maxMapHeight = 0;
        float minMapHeight = 0;
        (Vector3 key, NoiseData3D noise)[] noiseData = new (Vector3 key, NoiseData3D noise)[mapDimensions * mapDimensions];

        for (int x = 0, index = 0; x < mapDimensions; x++) {
            for (int y = 0; y < mapDimensions; y++) {
                for (int z = 0; z < mapDepth; z++, index++) {
                    Vector3 coord = new Vector3(x, z, y) * regionSize;

                    noiseData[index].key = coord;
                    noiseData[index].noise = GenerateNoise(seed,
                                                           scale,
                                                           noiseDimensions,
                                                           coord + mapOffset,
                                                           lacunarity,
                                                           persistence,
                                                           octaves,
                                                           noiseMode,
                                                           noiseInterpolateMode,
                                                           noiseLocalInterpolateMode
                    );


                    if (maxMapHeight < noiseData[index].noise.localMax)
                        maxMapHeight = noiseData[index].noise.localMax;
                    if (minMapHeight > noiseData[index].noise.localMin)
                        minMapHeight = noiseData[index].noise.localMin;
                }
            }
        }

        // Try to normalize noise retroactively
        for (int i = 0; i < noiseData.Length; i++)
            noiseData[i].noise = NormalizeNoise(noiseData[i].noise, minMapHeight, maxMapHeight);    

        //Fix Generate MeshData3D
        foreach ((Vector3 key, NoiseData3D noise) entry in noiseData) {
            MeshData meshData = GenerateMeshData(entry.noise.map, noiseDimensions, threshhold, textureDetail, meshDetail);
            Color[] colorMap = ChooseColorMap(entry.noise.map);
            MapData3D mapData = new MapData3D(noiseDimensions, entry.noise, meshData, colorMap);

            regions.Add(entry.key, new Region(mapData, entry.key, mapWrapper.transform, meshMaterial));
        }    
    }

    private Color[] ChooseColorMap(float[,] map, RegionType regionType) {
        Color[] output = null;
        //For now will implement biomes later
        RegionType _default = GetRegionType("standard");

        switch(drawMode) {
            case DrawMode.ColorMap:
                output = GenerateForColorMap(noiseDimensions, map, color);
                break;
            case DrawMode.TerrainMap:
                output = GenerateForTerrainMap(noiseDimensions, map, _default.terrainTypes); 
                break;
        }

        return output;
    }

    private Color[] ChooseColorMap(float[,,] map) {
        Color[] output = new Color[noiseDimensions * noiseDimensions * noiseDimensions];

        //switch (drawMode) {
        //    case DrawMode.ColorMap:
        //        output = GenerateForColorMap(noiseDimensions, map, color);
        //        break;
        //    case DrawMode.TerrainMap:
        //        output = GenerateForTerrainMap(noiseDimensions, map, terrainTypes);
        //        break;
        //}

        for (int x = 0; x < noiseDimensions; x++) {
            for (int y = 0; y < noiseDimensions; y++) {
                for (int z = 0; z < noiseDimensions; z++) {
                    output[x + (y * noiseDimensions) + z * (noiseDimensions * noiseDimensions)] = Color.red;
                }
            }
        }

        return output;
    }

    private RegionType GetRegionType(string regionName) {
        RegionType output;
        XDocument source = XDocument.Parse(Resources.Load<TextAsset>("Data/region_types").text);

        //Query for TerrainTypes by regionName
        IEnumerable<XElement> iEnumerable = from regionType in source.Root.Descendants("regionType")
                                            where regionType.Attribute("name").Value.Equals(regionName)
                                            from terrainType in regionType.Element("terrainTypes").Elements("terrainType")
                                            select terrainType;

        List<XElement> xmlTerrainTypes = iEnumerable.ToList();

        TerrainType[] terrainTypes = new TerrainType[xmlTerrainTypes.Count];

        for (int i = 0; i < xmlTerrainTypes.Count; i++) {
            XElement xmlColor = xmlTerrainTypes[i].Element("color");
            Color color = new Color(float.Parse(xmlColor.Attribute("r").Value) / 255f,
                                    float.Parse(xmlColor.Attribute("g").Value) / 255f,
                                    float.Parse(xmlColor.Attribute("b").Value) / 255f);

            terrainTypes[i] = new TerrainType(xmlTerrainTypes[i].Attribute("name").Value,
                                              float.Parse(xmlTerrainTypes[i].Element("maxHeight").Value),
                                              float.Parse(xmlTerrainTypes[i].Element("minHeight").Value),
                                              color);
        }

        //Query for RegionType by regionName
        iEnumerable = from regionType in source.Root.Descendants("regionType")
                      where regionType.Attribute("name").Value.Equals(regionName)
                      select regionType;

        XElement xmlRegionType = iEnumerable.FirstOrDefault();

        output = new RegionType(xmlRegionType.Attribute("name").Value, terrainTypes);

        return output;
    }

    private void OnValidate() {
        // NoiseSettings
        if (lacunarity < 1) { lacunarity = 1; }
        if (octaves < 1) { octaves = 1; }

        // MeshSettings
        if (meshDetail < 5) { meshDetail = 5; }
        while ((noiseDimensions - 1) % meshDetail != 0) { meshDetail++; }
        if (heightModifier < 0) { heightModifier = 0; }
    }
}