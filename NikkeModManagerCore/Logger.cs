using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NikkeModManagerCore {
    public static class Logger {

        // TODO: Make this not terrible

        private static string _logFile = "";

        static readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

        private static Timer _timer;

        public static void Initialize(string file) {
            _logFile = file;
            if(File.Exists(_logFile)) File.Delete(_logFile);
            File.Create(_logFile).Close();
            _timer = new Timer(_ => LogMessages(), null, 500, 500);
            WriteLine($"Initializing Logger to {Path.Join(Directory.GetCurrentDirectory(),_logFile)}");

        }

        public static void WriteLine(object message) {
            Console.WriteLine(message);
            _messageQueue.Enqueue(message + "\n");
        }

        static void LogMessages() {
            while (_messageQueue.Count > 0) {
                try {
                    _messageQueue.TryDequeue(out string text);
                    File.AppendAllText(_logFile, text);
                } catch {
                    return;
                }
            }
        }
    }
}
