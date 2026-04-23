using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace finalProject
{
    internal static class Scanner
    {
        private static bool _isPaused = false;
        private static Dictionary<string, int> _foundWordsCount = new Dictionary<string, int>();
        static int progressBarRow;

        public static void StartScan()
        {
            if (Options.Words.Count == 0)
            {
                Console.WriteLine("Спочатку введіть заборонені слова!");
                return;
            }

            Options.EnsureOutputDefault(); 
            Status.Reset();               
            _foundWordsCount.Clear();
            _isPaused = false;

            try
            {
                Directory.CreateDirectory(Options.Output);

             
                string outFolder = Path.Combine(Options.Output, "redacted");
                Directory.CreateDirectory(outFolder);

               
                string reportName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string reportPath = Path.Combine(Options.Output, reportName);

              
                Console.WriteLine("\n--- СТАРТ СКАНУВАННЯ ---");
                Console.WriteLine($"Папка зі звітами: {Path.GetFullPath(Options.Output)}");
                Console.WriteLine($"Змінені файли будуть тут: {outFolder}");

                new Thread(Control) { IsBackground = true }.Start();

               
                DoScan(reportPath);

                Status.Cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при створенні папок: {ex.Message}");
            }
        }

        static void Control()
        {
            while (!Status.Cts.IsCancellationRequested)
            {
                if (!Console.KeyAvailable) { Thread.Sleep(100); continue; }
                var k = Console.ReadKey(true).Key;
                if (k == ConsoleKey.P)
                {
                    _isPaused = !_isPaused;
                    if (_isPaused)
                        Console.WriteLine("\nПАУЗА");
                    else
                        Console.WriteLine("\nПРОДОВЖЕНО");
                }
                else if (k == ConsoleKey.S)
                {
                    Status.Cts.Cancel();
                    _isPaused = false;
                    Console.WriteLine("\nЗУПИНЕНО");
                }
            }
        }

        static void DoScan(string reportPath)
        {
            DateTime startTime = DateTime.Now;
            File.WriteAllText(reportPath, "----- Word Scanner - Звіт -----\nДата: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "\n");

            try
            {
                if (Options.SearchPath != "")
                {
                    if (Directory.Exists(Options.SearchPath))
                        ScanFolder(Options.SearchPath, reportPath);
                    else
                        Console.WriteLine($"Папка не існує: {Options.SearchPath}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"Помилка: {ex.Message}"); }

            PrintResults(DateTime.Now - startTime, reportPath);
        }

        static void ProcessFile(string path, string reportPath)
        {
            try
            {
                string content = File.ReadAllText(path);
                char[] letters = content.ToCharArray();
                int totalInFile = 0;
                FileInfo fileInfo = new FileInfo(path); 

                foreach (string word in Options.Words)
                {
                    if (string.IsNullOrEmpty(word)) continue;
                    int wordCount = 0;

                    for (int i = 0; i <= letters.Length - word.Length; i++)
                    {
                        bool match = true;

                        for (int j = 0; j < word.Length; j++)
                        {
                            char cText = letters[i + j];
                            char cWord = word[j];

                            if (Options.Register)
                            {
                                if (cText != cWord) { match = false; break; }
                            }
                            else
                            {
                               
                                if (cText != cWord &&
                                    char.ToUpper(cText) != char.ToUpper(cWord))
                                {
                                    match = false; break;
                                }
                            }
                        }

                        if (match)
                        {
                            
                            for (int k = 0; k < word.Length; k++)
                            {
                                letters[i + k] = '*';
                            }

                            wordCount++;
                            totalInFile++;
                            i += word.Length - 1;
                        }
                    }

                    if (wordCount > 0)
                    {
                        lock (_foundWordsCount)
                        {
                            if (!_foundWordsCount.ContainsKey(word)) _foundWordsCount[word] = wordCount;
                            else _foundWordsCount[word] += wordCount;
                        }
                    }
                }

                if (totalInFile > 0)
                {
                    string originalFolder = Path.Combine(Options.Output, "Originals");
                    Directory.CreateDirectory(originalFolder);
                    File.Copy(path, Path.Combine(originalFolder, Path.GetFileName(path)), true);

                    string redactedText = new string(letters);

                    string redactedFolder = Path.Combine(Options.Output, "Redacted");
                    Directory.CreateDirectory(redactedFolder);
                    File.WriteAllText(Path.Combine(redactedFolder, "redacted_" + Path.GetFileName(path)), redactedText);

                    lock (reportPath)
                    {
                        string info = $"Файл: {path}\n" +
                                      $"Розмір: {fileInfo.Length} байт\n" +
                                      $"Замін: {totalInFile}\n" +
                                      $"-----------------------------\n";
                        File.AppendAllText(reportPath, info);
                    }

                    lock (Status.ConsoleLock) { Status.Found++; Status.Replacements += totalInFile; }
                }
                lock (Status.ConsoleLock) { Status.Scanned++; }
            }
            catch {  }
        }

        static void ScanFolder(string folder, string reportPath)
        {
            List<string> files = FindFiles(folder);
            Console.WriteLine($"Файлів для перевірки: {files.Count}");
            progressBarRow = Console.CursorTop;
            Console.WriteLine();

            List<Task> tasks = new List<Task>();
            foreach (string f in files)
            {
                string fileCopy = f;
                tasks.Add(Task.Run(() =>
                {
                    while (_isPaused && !Status.Cts.IsCancellationRequested) Thread.Sleep(200);
                    if (Status.Cts.IsCancellationRequested) return;

                    ProcessFile(fileCopy, reportPath);

                    lock (Status.ConsoleLock)
                    {
                        ShowProgress(Status.Scanned, files.Count);
                    }
                }));
            }
            try { Task.WaitAll(tasks.ToArray(), Status.Cts.Token); } catch { }
        }

        static List<string> FindFiles(string root)
        {
            List<string> list = new List<string>();
            try
            {
                foreach (string ext in Options.Exts)
                    foreach (string f in Directory.EnumerateFiles(root, "*" + ext, SearchOption.AllDirectories))
                        list.Add(f);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return list;
        }

        static void ShowProgress(long done, long total)
        {
            if (total == 0) return;
            lock (Status.ConsoleLock)
            {
                int width = 50;
                int percent = (int)((double)done / total * 100);
                int filled = percent * width / 100;
                string bar = "[" + new string('#', filled) + new string(' ', width - filled) + "] " + percent + "% (" + done + "/" + total + ")";

                int left = Console.CursorLeft, top = Console.CursorTop;
                Console.SetCursorPosition(0, progressBarRow);
                Console.Write(bar);
                Console.SetCursorPosition(left, top);
            }
        }

        static void PrintResults(TimeSpan elapsed, string reportPath)
        {
            var top10 = new List<string>(_foundWordsCount.Keys);
            top10.Sort((a, b) => _foundWordsCount[b].CompareTo(_foundWordsCount[a]));
            if (top10.Count > 10) top10 = top10.GetRange(0, 10);

            Console.WriteLine($"\n--- РЕЗУЛЬТАТИ ---\nПеревірено: {Status.Scanned}\nЗнайдено: {Status.Found}\nЗамін: {Status.Replacements}\nЧас: {elapsed:hh\\:mm\\:ss}");

            if (top10.Count > 0)
            {
                Console.WriteLine("ТОП слів:");
                for (int i = 0; i < top10.Count; i++)
                    Console.WriteLine($"  {i + 1}. {top10[i]}: {_foundWordsCount[top10[i]]}");
            }
        }
    }
}
