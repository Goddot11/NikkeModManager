using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NikkeModManagerCore {
    public static class NikkeDataHelper {
        public const string NIKKE_DATA_FILE = "_nikke_data.csv";
        public const string NAME_DATA_FILE = "_file_data.csv";

        private static Dictionary<string, NameRow> _nikkeRows = new Dictionary<string, NameRow>();
        private static Dictionary<string, string> _fileRows = new Dictionary<string, string>();

        public static void Initialize() {
            string[] data = File.ReadAllLines(NIKKE_DATA_FILE);
            IEnumerable<NameRow> rowData = data.ToList().Select(line => new NameRow(line));
            _nikkeRows = rowData.ToList().ToDictionary(row => row.Id, row => row);
            Console.WriteLine($"Loaded {_nikkeRows.Count} rows of nikke data, skipping {_nikkeRows.Values.Count(x => x.Skip)} bundles");

            if (File.Exists(NAME_DATA_FILE)) {
                data = File.ReadAllLines(NAME_DATA_FILE);
                _fileRows = data.ToDictionary(line => line.Split(",")[0], line => line.Split(",")[^1]);
                Console.WriteLine($"Loaded {data.Length} filename mappings");
            } else {
                _fileRows = new Dictionary<string, string>();
                Console.WriteLine("Unable to find Nikke Filename mapping");
            }
        }

        public static string GetName(string characterId) => _nikkeRows.ContainsKey(characterId) ? _nikkeRows[characterId].Name : "Entry Not Found";
        public static bool ShouldSkip(string characterId) => !_nikkeRows.ContainsKey(characterId) || _nikkeRows[characterId].Skip;

        public static string GetFilename(string fileHash) => _fileRows.ContainsKey(fileHash) ? _fileRows[fileHash] : "";
        public static string SetFilename(string fileHash, string filename) => _fileRows[fileHash] = filename;
        public static void SaveData() {
            List<string> data = _fileRows.Select(q => q.Key+","+q.Value).ToList();
            if(File.Exists(NAME_DATA_FILE)) File.Delete(NAME_DATA_FILE);
            File.WriteAllLines(NAME_DATA_FILE, data);
        }

        private class NameRow {
            public string Id { get; set; }
            public string Name { get; set; }
            public bool Skip { get; set; }

            public NameRow(string row) {
                var parts = row.Split(',');
                Id = parts[0];
                Name = parts[1];
                Skip = parts[2] != "";
            }
        }
    }
}
