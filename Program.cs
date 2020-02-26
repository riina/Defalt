using System;
using System.IO;

namespace Defalt {
    internal static class Program {
        private static void Main(string[] args) {
            Console.WriteLine("Voice wrecker sample program."
                              + "\nHint: pass \"-x\" to avoid saving your cringe."
                              + "\nHint: pass \"-t <0.0-1.0>\" to set pitchy voice threshold (default 0.4)."
                              + "\nHint: pass \"-i <wav>\" to stream / convert an audio file.");
            var instance = new Instance();
            var threshold = 0.4f;
            string inPath = null;
            var noSave = false;
            for (var i = 0; i < args.Length; i++) {
                switch (args[i].ToLowerInvariant()) {
                    case "-i" when i < args.Length - 1:
                        inPath = args[i + 1];
                        break;
                    case "-t" when i < args.Length - 1:
                        threshold = float.Parse(args[i + 1]);
                        break;
                    case "-x":
                        noSave = true;
                        break;
                }
            }

            Console.WriteLine("Press Enter to start.");
            while (Console.ReadKey(true).Key != ConsoleKey.Enter) {
            }

            instance.Start(inPath, threshold, 0, 0.1f, noSave ? default : Gen("."));
            Console.WriteLine("Press CTRL-C to end.");
            Console.ReadKey(true);
            ConsoleKeyInfo k;
            while ((k = Console.ReadKey(true)).Key != ConsoleKey.C ||
                   (k.Modifiers & ConsoleModifiers.Control) != ConsoleModifiers.Control) {
            }

            instance.Stop();
        }

        private static string Gen(string dir) {
            var full = Path.Combine(dir, "output.wav");
            var i = 0;
            while (File.Exists(full))
                full = Path.Combine(dir, $"output_{i++}.wav");
            return full;
        }
    }
}