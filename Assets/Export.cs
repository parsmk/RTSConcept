using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class Export {
    public static async Task Export1DArray<T>(string fileName, T[] map, int dimensions) {
        try {
            string file = Path.Combine(Directory.GetCurrentDirectory(), "CSVs/1D", fileName + ".csv");

            if (!File.Exists(file))
                Debug.Log("Creating CSV for: " + file);
            else
                Debug.Log("Updating CSV for: " + file);

            StringBuilder sb = new StringBuilder();
            using (StreamWriter writer = new StreamWriter(file)) {
                string[] output = new string[dimensions];

                for (int i = 0; i < dimensions; i++) {
                    output[i] = map[i].ToString();
                }

                sb.AppendLine(string.Join(',', output));
                await writer.WriteAsync(sb.ToString());
                sb.Clear();
            }

            Debug.Log("Done CSV for: " + file);

        } catch (System.Exception e) {
            Debug.Log("Error exporting!\n" + e.Message);
        }
    }

    public static async Task Export2DArray<T>(string fileName, T[,] map, int dimensions) {
        try {
            string file = Path.Combine(Directory.GetCurrentDirectory(), "CSVs/2D", fileName + ".csv");

            if (!File.Exists(file))
                Debug.Log("Creating CSV for: " + file);
            else
                Debug.Log("Updating CSV for: " + file);

            StringBuilder sb = new StringBuilder();
            using (StreamWriter writer = new StreamWriter(file)) {
                for (int y = 0; y < dimensions; y++) {
                    string[] row = new string[dimensions];
                    for (int x = 0; x < dimensions; x++) {
                        row[x] = map[x, y].ToString();
                    }

                    sb.AppendLine(string.Join(",", row));
                }

                await writer.WriteAsync(sb.ToString());
            }

            Debug.Log("Done CSV for: " + file);

        } catch (System.Exception e) {
            Debug.Log("Error exporting!\n" + e.Message);
        }
    }

    public static async Task Export3DArray<T>(string fileName, T[,,] map, int dimensions) {
        try {
            for (int y = 0; y < dimensions; y++) {
                string file = Path.Combine(Directory.GetCurrentDirectory(), "CSVs/3D", fileName + "_map[y = " + y + "].csv");

                if (!File.Exists(file))
                    Debug.Log("Creating CSV for: " + file);
                else
                    Debug.Log("Updating CSV for: " + file);

                StringBuilder sb = new StringBuilder();
                using (StreamWriter writer = new StreamWriter(file)) {
                    for (int x = 0; x < dimensions; x++) {
                        string[] row = new string[dimensions];
                        for (int z = 0; z < dimensions; z++) {
                            row[z] = map[x, y, z].ToString();
                        }
                        sb.AppendLine(string.Join(',', row));
                    }

                    await writer.WriteAsync(sb.ToString());
                }

                Debug.Log("Done CSV for: " + file);
            }
        } catch (System.Exception e) {
            Debug.Log("Error exporting!\n" + e.Message);
        }
    }
}
