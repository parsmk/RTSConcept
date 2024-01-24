using CsvHelper;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class Export {
    public static async Task Export1DArray<T>(string fileName, T[] map, int dimensions) {
        try {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "CSVs/1D", fileName + ".csv");

            if (!Directory.Exists(directory))
                Debug.Log("Creating CSV for: " + directory);
            else
                Debug.Log("Updating CSV for: " + directory);

            using (StreamWriter writer = new StreamWriter(directory))
            using (CsvWriter csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture)) {
                for (int i = 0; i < dimensions; i++) {
                    csv.WriteField($"{map[i]}");
                    await csv.NextRecordAsync();
                }

            }

            Debug.Log("Done CSV for: " + directory);

        } catch (System.Exception e) {
            Debug.Log("Error exporting!\n" + e.Message);
        }
    }

    public static async Task Export2DArray<T>(string fileName, T[,] map, int dimensions) {
        try {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "CSVs/2D", fileName + ".csv");

            if (!Directory.Exists(directory))
                Debug.Log("Creating CSV for: " + directory);
            else
                Debug.Log("Updating CSV for: " + directory);

            using (StreamWriter writer = new StreamWriter(directory))
            using (CsvWriter csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture)) {
                for (int y = 0; y < dimensions; y++) {
                    for (int x = 0; x < dimensions; x++) {
                        csv.WriteField($"{map[x, y]}");
                    }
                    await csv.NextRecordAsync();
                }

            }

            Debug.Log("Done CSV for: " + directory);

        } catch (System.Exception e) {
            Debug.Log("Error exporting!\n" + e.Message);
        }

    }

    public static async Task Export3DArray<T>(string fileName, T[,,] map, int dimensions) {
        try {
            for (int y = 0; y < dimensions; y++) {
                string directory = Path.Combine(Directory.GetCurrentDirectory(), "CSVs/3D", fileName + "_map[y = " + y + "].csv");

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
        } catch (System.Exception e) {
            Debug.Log("Error exporting!\n" + e.Message);
        }
    }
}
