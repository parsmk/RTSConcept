﻿using CsvHelper;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using static NoiseGenerator;

public static class Export {

    #region MapExport
    public static async Task Export2DArray<T>(string fileName, T[,] map, int dimensions) {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "CSVs", fileName + ".csv");

        if (!Directory.Exists(directory))
            Debug.Log("Creating CSV for: " + directory);
        else
            Debug.Log("Updating CSV for: " + directory);

        using (StreamWriter writer = new StreamWriter(directory))
        using (CsvWriter csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture)) {
            for (int x = 0; x < dimensions; x++) {
                csv.WriteField(x);
            }
            await csv.NextRecordAsync();

            for (int y = 0; y < dimensions; y++) {
                for (int x = 0; x < dimensions; x++) {
                    csv.WriteField($"{map[x, y]}");
                }
                await csv.NextRecordAsync();
            }

        }

        Debug.Log("Done CSV for: " + directory);
    }

    public static async Task Export3DArray<T>(string fileName, T[,,] map, int dimensions) {
        for (int y = 0; y < dimensions; y++) {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "CSVs", fileName + "_map[ y: " + y + "].csv");

            if (!Directory.Exists(directory))
                Debug.Log("Creating CSV for: " + directory);
            else
                Debug.Log("Updating CSV for: " + directory);

            using (StreamWriter writer = new StreamWriter(directory))
            using (CsvWriter csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture)) {
                for (int x = 0; x < dimensions; x++) {
                    for (int z = 0; z < dimensions; z++) {
                        csv.WriteField($"{map[x, y, z]}");

                    }
                    await csv.NextRecordAsync();
                }
            }

            Debug.Log("Done CSV for: " + directory);
        }
    }

    #endregion
}
