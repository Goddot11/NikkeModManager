using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NikkeModManagerCore.Exceptions {
    public class GameDataNotFoundException : Exception {
        public GameDataNotFoundException(string message) : base(message) { }
    }
}
