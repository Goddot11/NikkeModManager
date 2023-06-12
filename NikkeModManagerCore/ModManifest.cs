using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NikkeModManagerCore {
    public class ModManifest {
        public string Author { get; set; } = "";
        public string Link { get; set; } = "";
        public string GameVersion { get; set; } = "";
        public string ModVersion { get; set; } = "";
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}
