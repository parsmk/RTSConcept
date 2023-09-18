using UnityEngine;

public static class NoiseGenerator {
    public enum NoiseInterpolateMode { Hermite, Lerp };
    public enum NoiseLocalInterpolateMode { SmoothStep, Fade };

    #region 3D
    public struct NoiseData3D {
        public int dimensions;
        public float localMax;
        public float localMin;
        public float[,,] map;

        public NoiseData3D(int dimensions, float localMax, float localMin, float[,,] map) {
            this.dimensions = dimensions;
            this.localMax = localMax;
            this.localMin = localMin;
            this.map = map;
        }

        public static implicit operator NoiseData3D(NoiseData2D noiseData) {
            return new NoiseData2D(noiseData.dimensions, noiseData.localMax, noiseData.localMin, noiseData.map);
        }
    }
    public static NoiseData3D GenerateNoise3D(
            int seed,
            float scale,
            int dimensions,
            Vector3 offset,
            float lacunarity,
            float persistence,
            int octaves,
            NoiseInterpolateMode interpolateMode,
            NoiseLocalInterpolateMode localInterpolateMode
    ) {

        float[,,] map = new float[dimensions, dimensions, dimensions];

        System.Random prng = new System.Random(seed);
        Vector3[] octaveOffsets = new Vector3[octaves];

        // Random points on the noiseMap are chosen to act as starting points then we add the custom offset.
        for (int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            float offsetZ = prng.Next(-100000, 100000) + offset.z;
            octaveOffsets[i] = new Vector3(offsetX, offsetY, offsetZ);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // Loop through all the points in the map
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                for (int z = 0; z < dimensions; z++) {
                    // Reset the amplitude, frequency and noiseHeight
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0f;

                    // For each layer of noise (octave)
                    for (int i = 0; i < octaves; i++) {
                        // perform f(x) = a*perlin(freq*x + c) solved for f(y) as well.
                        // sampleX = (freq*x + c)
                        float sampleX = frequency * (x + octaveOffsets[i].x) / scale;
                        // sampleY = (freq*y + c)
                        float sampleY = frequency * (y + octaveOffsets[i].y) / scale;
                        // sampleZ = (freq*z + c)
                        float sampleZ = frequency * (z + octaveOffsets[i].z) / scale;
                        // a * perlin(sampleX) && a * perlin(sampleY)
                        // *2 - 1 to include negative values
                        noiseHeight += amplitude * (Perlin3D(interpolateMode, localInterpolateMode, sampleX, sampleY, sampleZ) * 2 - 1);

                        //Change the amplitude and frequency based on a multiplier

                        //The amplitude will determine how gray a point will become. The higher this number is the more extreme the gray. i.e (x,y) -> 1 more black.
                        //Persistence will thus determine how regular the greys are.
                        amplitude *= persistence;
                        //The frequency adds more points the higher it is. This means more gray, black and white points.
                        //Lacunarity then further complicates this by incrementing frequency over a smaller area (octave).
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight) {
                        maxNoiseHeight = noiseHeight;
                    } else if (noiseHeight < minNoiseHeight) {
                        minNoiseHeight = noiseHeight;
                    }
                    map[x, y, z] = noiseHeight;
                }
            }
        }

        return new NoiseData3D(dimensions, maxNoiseHeight, minNoiseHeight, map);
    }

    public static NoiseData3D NormalizeNoise3D(NoiseData3D noiseData, float inputMin, float inputMax) {
        float[,,] normalizedMap = new float[noiseData.dimensions, noiseData.dimensions, noiseData.dimensions];
        for (int x = 0; x < noiseData.dimensions; x++) {
            for (int y = 0; y < noiseData.dimensions; y++) {
                for (int z = 0; z < noiseData.dimensions; z++) {
                    normalizedMap[x, y, z] = Mathf.InverseLerp(inputMin, inputMax, noiseData.map[x, y, z]);
                }
            }
        }

        return new NoiseData3D(noiseData.dimensions, inputMax, inputMin, normalizedMap);
    }

    private static float Perlin3D(
            NoiseInterpolateMode interpolateMode,
            NoiseLocalInterpolateMode localInterpolateMode,
            float x, float y, float z
    ) {
        if (permutationTable is null)
            GeneratePermutationTable();

        int uCubeX = (int)x & 255;
        int uCubeY = (int)y & 255;
        int uCubeZ = (int)z & 255;

        float deltaX = x - uCubeX;
        float deltaY = y - uCubeY;
        float deltaZ = z - uCubeZ;

        //Vector from Unit Cube vertex (x,y,z) to coordinate
        Vector3 vector000 = new Vector3(deltaX, deltaY, deltaZ);
        Vector3 vector100 = new Vector3(1 - deltaX, deltaY, deltaZ);
        Vector3 vector010 = new Vector3(deltaX, 1 - deltaY, deltaZ);
        Vector3 vector110 = new Vector3(1 - deltaX, 1 - deltaY, deltaZ);
        Vector3 vector001 = new Vector3(deltaX, deltaY, 1 - deltaZ);
        Vector3 vector101 = new Vector3(1 - deltaX, deltaY, 1 - deltaZ);
        Vector3 vector011 = new Vector3(deltaX, 1 - deltaY, 1 - deltaZ);
        Vector3 vector111 = new Vector3(1 - deltaX, 1 - deltaY, 1 - deltaZ);

        //randomValue from Unit Cube vertex (x,y,z)
        int value000 = permutationTable[ChoosePermutationIndex(uCubeX, uCubeY, uCubeZ)];
        int value100 = permutationTable[ChoosePermutationIndex(1 + uCubeX, uCubeY, uCubeZ)];
        int value010 = permutationTable[ChoosePermutationIndex(uCubeX, 1 + uCubeY, uCubeZ)];
        int value110 = permutationTable[ChoosePermutationIndex(uCubeX + 1, uCubeY + 1, uCubeZ)];
        int value001 = permutationTable[ChoosePermutationIndex(uCubeX, uCubeY, uCubeZ + 1)];
        int value101 = permutationTable[ChoosePermutationIndex(uCubeX + 1, uCubeY, uCubeZ + 1)];
        int value011 = permutationTable[ChoosePermutationIndex(uCubeX, uCubeY + 1, uCubeZ + 1)];
        int value111 = permutationTable[ChoosePermutationIndex(uCubeX + 1, uCubeY + 1, uCubeZ + 1)];

        //Dot product of vector pointing to grid point and constant vector determined by randomValue
        float dot000 = Vector3.Dot(vector000, NoiseConstants.constantVectors3D[(value000 & 15) % 12 - 1]);
        float dot100 = Vector3.Dot(vector100, NoiseConstants.constantVectors3D[(value100 & 15) % 12 - 1]);
        float dot010 = Vector3.Dot(vector010, NoiseConstants.constantVectors3D[(value010 & 15) % 12 - 1]);
        float dot110 = Vector3.Dot(vector110, NoiseConstants.constantVectors3D[(value110 & 15) % 12 - 1]);
        float dot001 = Vector3.Dot(vector001, NoiseConstants.constantVectors3D[(value001 & 15) % 12 - 1]);
        float dot101 = Vector3.Dot(vector101, NoiseConstants.constantVectors3D[(value101 & 15) % 12 - 1]);
        float dot011 = Vector3.Dot(vector011, NoiseConstants.constantVectors3D[(value011 & 15) % 12 - 1]);
        float dot111 = Vector3.Dot(vector111, NoiseConstants.constantVectors3D[(value111 & 15) % 12 - 1]);

        float smoothDeltaX = LocalInterpolate(localInterpolateMode, deltaX);
        float smoothDeltaY = LocalInterpolate(localInterpolateMode, deltaY);
        float smoothDeltaZ = LocalInterpolate(localInterpolateMode, deltaZ);

        float interpolatedBottomCloseEdge = Interpolate(interpolateMode, dot000, dot100, smoothDeltaX);
        float interpolatedTopCloseEdge = Interpolate(interpolateMode, dot010, dot110, smoothDeltaX);
        float interpolatedBottomFarEdge = Interpolate(interpolateMode, dot001, dot101, smoothDeltaX);
        float interpolatedTopFarEdge = Interpolate(interpolateMode, dot011, dot111, smoothDeltaX);

        float interpolatedCloseFace = Interpolate(interpolateMode, interpolatedBottomCloseEdge, interpolatedTopCloseEdge, smoothDeltaY);
        float interpolatedFarFace = Interpolate(interpolateMode, interpolatedBottomFarEdge, interpolatedTopFarEdge, smoothDeltaY);

        return Interpolate(interpolateMode, interpolatedCloseFace, interpolatedFarFace, smoothDeltaZ);
    }
    #endregion

    #region 2D
    public struct NoiseData2D {
        public int dimensions;
        public float localMax;
        public float localMin;
        public float[,] map;

        public NoiseData2D(int dimensions, float localMax, float localMin, float[,] map) {
            this.dimensions = dimensions;
            this.localMax = localMax;
            this.localMin = localMin;
            this.map = map;
        }
    }

    public static NoiseData2D GenerateNoise2D(
            int seed,
            float scale,
            int dimensions,
            Vector2 offset,
            float lacunarity,
            float persistence,
            int octaves,
            NoiseInterpolateMode interpolateMode,
            NoiseLocalInterpolateMode localInterpolateMode
    ) {

        float[,] map = new float[dimensions, dimensions];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        // Random points on the noiseMap are chosen to act as starting points then we add the custom offset.
        for (int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // Loop through all the points in the map
        for (int y = 0; y < dimensions; y++) {
            for (int x = 0; x < dimensions; x++) {
                // Reset the amplitude, frequency and noiseHeight
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0f;

                // For each layer of noise (octave)
                for (int i = 0; i < octaves; i++) {
                    // perform f(x) = a*perlin(freq*x + c) solved for f(y) as well.
                    // sampleX = (freq*x + c)
                    float sampleX = frequency * (x + octaveOffsets[i].x) / scale;
                    // sampleY = (freq*y + c)
                    float sampleY = frequency * (y + octaveOffsets[i].y) / scale;
                    // a * perlin(sampleX) && a * perlin(sampleY)
                    // *2 - 1 to include negative values
                    noiseHeight += amplitude * (Perlin2D(interpolateMode, localInterpolateMode, sampleX, sampleY) * 2 - 1);

                    //Change the amplitude and frequency based on a multiplier

                    //The amplitude will determine how gray a point will become. The higher this number is the more extreme the gray. i.e (x,y) -> 1 more black.
                    //Persistence will thus determine how regular the greys are.
                    amplitude *= persistence;
                    //The frequency adds more points the higher it is. This means more gray, black and white points.
                    //Lacunarity then further complicates this by incrementing frequency over a smaller area (octave).
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight) {
                    maxNoiseHeight = noiseHeight;
                } else if (noiseHeight < minNoiseHeight) {
                    minNoiseHeight = noiseHeight;
                }
                map[x, y] = noiseHeight;
            }
        }

        return new NoiseData2D(dimensions, maxNoiseHeight, minNoiseHeight, map);
    }

    private static float Perlin2D(
            NoiseInterpolateMode interpolateMode,
            NoiseLocalInterpolateMode localInterpolateMode,
            float x, float y
    ) {
        if (permutationTable is null)
            GeneratePermutationTable();

        int uCubeX = (int)x & 255;
        int uCubeY = (int)y & 255;

        float deltaX = x - uCubeX;
        float deltaY = y - uCubeY;

        //Vector from Unit Cube vertex (x,y,z) to coordinate
        Vector2 vector00 = new Vector2(deltaX, deltaY);
        Vector2 vector10 = new Vector2(1 - deltaX, deltaY);
        Vector2 vector01 = new Vector2(deltaX, 1 - deltaY);
        Vector2 vector11 = new Vector2(1 - deltaX, 1 - deltaY);

        //randomValue from Unit Cube vertex (x,y,z)
        int value00 = permutationTable[ChoosePermutationIndex(uCubeX, uCubeY, uCubeX + uCubeY % 255)];
        int value10 = permutationTable[ChoosePermutationIndex(1 + uCubeX, uCubeY, 1 + uCubeX + uCubeY % 255)];
        int value01 = permutationTable[ChoosePermutationIndex(uCubeX, 1 + uCubeY, 1 + uCubeX + uCubeY % 255)];
        int value11 = permutationTable[ChoosePermutationIndex(uCubeX + 1, uCubeY + 1, 2 + uCubeX + uCubeY % 255)];

        //Dot product of vector pointing to grid point and constant vector determined by randomValue
        float dot00 = Vector3.Dot(vector00, NoiseConstants.constantVectors3D[value00 & 3]);
        float dot10 = Vector3.Dot(vector10, NoiseConstants.constantVectors3D[value10 & 3]);
        float dot01 = Vector3.Dot(vector01, NoiseConstants.constantVectors3D[value01 & 3]);
        float dot11 = Vector3.Dot(vector11, NoiseConstants.constantVectors3D[value11 & 3]);

        float smoothDeltaX = LocalInterpolate(localInterpolateMode, deltaX);
        float smoothDeltaY = LocalInterpolate(localInterpolateMode, deltaY);

        float interpolatedHorizontalEdg = Interpolate(interpolateMode, dot00, dot10, smoothDeltaX);
        float interpolatedVerticalEdge = Interpolate(interpolateMode, dot01, dot11, smoothDeltaX);

        return Interpolate(interpolateMode, interpolatedHorizontalEdg, interpolatedVerticalEdge, smoothDeltaY);
    }
    #endregion

    //TODO
    private static float Simplex(
            NoiseInterpolateMode interpolateMode,
            NoiseLocalInterpolateMode localInterpolateMode,
            float x, float y, float z
        ) {
        return 0f;
    }

    private static float Interpolate(NoiseInterpolateMode mode, float a, float b, float t) {
        float output = 0f;
        switch (mode) {
            case (NoiseInterpolateMode.Hermite):
                output = HermiteInterpolate(a, b, t);
                break;
            case (NoiseInterpolateMode.Lerp):
                output = Mathf.Lerp(a, b, t);
                break;
        }

        return output;
    }

    private static float HermiteInterpolate(float a, float b, float t) {
        float h1 = 2 * t * t * t - 3 * t * t + 1;
        float h2 = -2 * t * t * t + 3 * t * t;

        return h1 * a + h2 * b;
    }

    private static float LocalInterpolate(NoiseLocalInterpolateMode mode, float t) {
        float output = 0f;
        switch (mode) {
            case NoiseLocalInterpolateMode.Fade:
                output = Fade(t);
                break;
            case NoiseLocalInterpolateMode.SmoothStep:
                output = SmoothStep(t);
                break;
        }

        return output;
    }

    private static float Fade(float t) {
        return ((6 * t - 15) * t + 10) * t * t * t;
    }

    private static float SmoothStep(float t) {
        return t * t * (3.0f - 2.0f * t);
    }

    private static int ChoosePermutationIndex(int x, int y, int z) {
        return (x + y * z) % 511;
    }

    private static int[] permutationTable;
    private static void GeneratePermutationTable() {
        permutationTable = new int[512];

        for (int i = 0; i < 256; i++) {
            permutationTable[i] = i;
        }

        for (int i = 255; i > 0; i--) {
            int index = UnityEngine.Random.Range(0, i);

            int temp = permutationTable[i];
            permutationTable[i] = permutationTable[index];
            permutationTable[index] = permutationTable[i];
        }

        for (int i = 0; i < 256; i++) {
            permutationTable[i + 256] = permutationTable[i];
        }
    }
}