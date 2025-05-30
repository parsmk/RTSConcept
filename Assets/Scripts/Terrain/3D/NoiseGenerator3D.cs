﻿using UnityEngine;
using static NoiseHandler2D;

public class NoiseGenerator3D {
    public enum NoiseInterpolateMode3D { Hermite, Lerp };
    public enum NoiseLocalInterpolateMode3D { SmoothStep, Fade };
    public enum NoiseMode3D { UnityPerlin, CustomPerlin, Simplex, Worley };

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
    }

    public static NoiseData3D Fractal(
        int seed,
        float scale,
        int dimensions,
        Vector3 offset,
        float lacunarity,
        float persistence,
        int octaves,
        NoiseMode3D noiseMode,
        NoiseInterpolateMode3D interpolateMode,
        NoiseLocalInterpolateMode3D localInterpolateMode
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
                        noiseHeight += amplitude * ((Noise(noiseMode, interpolateMode, localInterpolateMode, sampleX, sampleY, sampleZ) * 2) - 1);

                        //Change the amplitude and frequency based on a multiplier

                        //The amplitude will determine how gray a point will become. The higher this number is the more extreme the gray. y.e (x,y) -> 1 more black.
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
    
    private static float Noise(
        NoiseMode3D noiseMode,
        NoiseInterpolateMode3D interpolateMode,
        NoiseLocalInterpolateMode3D localInterpolateMode,
        float x, float y, float z
     ) {
        float output = 0f;

        if (z == -1) {
            switch (noiseMode) {
                case NoiseMode3D.UnityPerlin:
                    return Mathf.PerlinNoise(x, y);
                case NoiseMode3D.CustomPerlin:
                    return Perlin(interpolateMode, localInterpolateMode, x, y, z);
                case NoiseMode3D.Simplex:
                    return Simplex(interpolateMode, localInterpolateMode, x, y, z);
            }
        } else {
            switch (noiseMode) {
                case NoiseMode3D.UnityPerlin:
                    return Mathf.PerlinNoise(x, y);
                case NoiseMode3D.CustomPerlin:
                    return Perlin(interpolateMode, localInterpolateMode, x, y, z);
                case NoiseMode3D.Simplex:
                    return Simplex(interpolateMode, localInterpolateMode, x, y, z);
            }
        }

        return output;
    }

    public static NoiseData3D NormalizeNoise(NoiseData3D noiseData, float inputMin, float inputMax) {
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

    private static float Perlin(
            NoiseInterpolateMode3D interpolateMode,
            NoiseLocalInterpolateMode3D localInterpolateMode,
            float x, float y, float z
    ) {
        if (permutationTable is null)
            GeneratePermutationTable();

        int uCubeX = (int)Mathf.Floor(x) & 255;
        int uCubeY = (int)Mathf.Floor(y) & 255;
        int uCubeZ = (int)Mathf.Floor(z) & 255;

        float deltaX = x - Mathf.Floor(x);
        float deltaY = y - Mathf.Floor(y);
        float deltaZ = z - Mathf.Floor(z);

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
        float dot000 = Vector3.Dot(vector000, NoiseConstants.constantVectors[(value000 & 7) % 12]);
        float dot100 = Vector3.Dot(vector100, NoiseConstants.constantVectors[(value100 & 7) % 12]);
        float dot010 = Vector3.Dot(vector010, NoiseConstants.constantVectors[(value010 & 7) % 12]);
        float dot110 = Vector3.Dot(vector110, NoiseConstants.constantVectors[(value110 & 7) % 12]);
        float dot001 = Vector3.Dot(vector001, NoiseConstants.constantVectors[(value001 & 7) % 12]);
        float dot101 = Vector3.Dot(vector101, NoiseConstants.constantVectors[(value101 & 7) % 12]);
        float dot011 = Vector3.Dot(vector011, NoiseConstants.constantVectors[(value011 & 7) % 12]);
        float dot111 = Vector3.Dot(vector111, NoiseConstants.constantVectors[(value111 & 7) % 12]);

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


    private static float Simplex(
            NoiseInterpolateMode3D interpolateMode,
            NoiseLocalInterpolateMode3D localInterpolateMode,
            float x, float y, float z
    ) {
        return 0f;
    }


    private static float Worley(Vector3[] seedPoints, int x, int y, int z) {
        Vector3 currentPoint = new Vector3(x, y, z);
        float minDist = float.MaxValue;

        foreach (Vector3 seedPoint in seedPoints) {
            float dist = Vector3.Distance(currentPoint, seedPoint);

            if (dist < minDist) { minDist = dist; }
        }

        return minDist;
    }

    #region Local Interpolation Methods
    private static float LocalInterpolate(NoiseLocalInterpolateMode3D mode, float t) {
        float output = 0f;
        switch (mode) {
            case NoiseLocalInterpolateMode3D.Fade:
                output = Fade(t);
                break;
            case NoiseLocalInterpolateMode3D.SmoothStep:
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
    #endregion

    #region Global Interpolation Methods
    private static float Interpolate(NoiseInterpolateMode3D mode, float a, float b, float t) {
        float output = 0f;
        switch (mode) {
            case (NoiseInterpolateMode3D.Hermite):
                output = HermiteInterpolate(a, b, t);
                break;
            case (NoiseInterpolateMode3D.Lerp):
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
    #endregion

    #region Helper functions
    private static int ChoosePermutationIndex(int x, int y, int z) {
        return (x + y * z) % 511;
    }

    private static int[] permutationTable;

    private static void GeneratePermutationTable() {
        permutationTable = new int[512];

        for (int i = 0; i < 256; i++) {
            permutationTable[i] = i;
        }

        for (int i = 256; i > 0; i--) {
            int index = UnityEngine.Random.Range(0, i);

            int temp = permutationTable[i];
            permutationTable[i] = permutationTable[index];
            permutationTable[index] = temp;
        }

        for (int i = 0; i < 256; i++) {
            permutationTable[i + 256] = permutationTable[i];
        }
    }
    #endregion
}