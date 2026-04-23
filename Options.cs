using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace finalProject
{
    internal static class Options
    {
        public static List<string> Words = new();
        public static string Output = "";
        public static string SearchPath = "";
        public static string[] Exts = { ".txt", ".cs", ".xml", ".json", ".log", ".md" };
        public static bool Register = true;

        public static void SwitchRegister()
        {
            Register = !Register;
        }

        public static void LoadWordsFromFile(string path)
        {
            Words = File.ReadAllLines(path)
                .Where(l => l.Length > 0)
                .ToList();
        }

        public static void SetOutput(string path)
        {
            if (path != "")
                Output = path;
        }

        public static void SetSearchPath(string path)
        {
            if (path != "")
                SearchPath = path;
        }

        public static void SetExtensions(string line)
        {
            var parts = line.Split(',')
                            .Where(x => x.StartsWith("."))
                            .ToArray();
            if (parts.Length > 0)
                Exts = parts;
        }

        public static void EnsureOutputDefault()
        {
            if (Output == "")
                Output = Directory.GetCurrentDirectory() + "\\WordScannerOutput";
        }
    }
}
