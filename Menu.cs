namespace finalProject
{
    internal static class Menu
    {
        public static void Run()
        {
            while (true)
            {
                PrintMenu();
                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": InputWords(); break;
                    case "2": LoadWordsFile(); break;
                    case "3": SetSearchPath(); break;
                    case "4": SetOutput(); break;
                    case "5": SetExtensions(); break;
                    case "6": Registr(); break;
                    case "7": Scanner.StartScan(); break;
                    case "8":
                        Console.WriteLine("До побачення!");
                        return;
                    default:
                        Console.WriteLine("Невірний вибір.");
                        break;
                }
            }
        }

        static void PrintMenu()
        {
            Console.WriteLine();
            Console.WriteLine("-----------МЕНЮ-------------");
            Console.WriteLine("1)Ввести слова вручну");
            Console.WriteLine("2)Завантажити слова з файлу");
            Console.WriteLine("3)Папка пошуку");
            Console.WriteLine("4)Папка результатів");
            Console.WriteLine("5)Розширення(.txt,.cs,.xml,.json,.log,.md)");
            Console.WriteLine("6) Регістр");
            Console.WriteLine("7)ЗАПУСТИТИ СКАНУВАННЯ");
            Console.WriteLine("8)Вийти");
            Console.Write("Вибір: ");
        }


        static void InputWords()
        {
            Console.WriteLine("Введіть заборонені слова: ");
            Options.Words.Clear();
            while (true)
            {
                Console.Write($" Слово: {Options.Words.Count + 1}: ");
                string? word = Console.ReadLine();
                if (word == null || word == "")break;
                Options.Words.Add(word);
                Console.WriteLine($"  Додано: {word}");
            }
            Console.WriteLine($"Всього слів: {Options.Words.Count}");
        }
        static void LoadWordsFile()
        {
            Console.Write("Шлях до файлу зі словами: ");
            string? path = Console.ReadLine();
            if (!File.Exists(path))
            { 
                Console.WriteLine("Файл не знайдено"); 
                return; 
            }
            Options.LoadWordsFromFile(path!);
            Console.WriteLine($"Завантажено {Options.Words.Count} слів.");
        }

        static void SetSearchPath()
        {
            Console.Write("Папка де шукати: ");
            string? path = Console.ReadLine();

            if (path == "" || path == null )
                Options.SearchPath = "";
            else
                Options.SearchPath = path;

            if (Options.SearchPath == "")
                Console.WriteLine("Буде шукати по всьому диску.");
            else
                Console.WriteLine($"Встановлено: {Options.SearchPath}");
        }

        static void SetOutput()
        {
            Console.Write("Папка для результатів: ");
            string? path = Console.ReadLine();
            if (path != null) Options.SetOutput(path);
            Console.WriteLine($"Встановлено: {Options.Output}");
        }


        static void SetExtensions()
        {
            Console.Write("Розширення через кому (.txt,.cs,.log): ");
            string? line = Console.ReadLine();
            if (line != "" && line != null) Options.SetExtensions(line);
            Console.Write("Встановлено: ");
            foreach (string ext in Options.Exts)
                Console.Write(ext + " ");
            Console.WriteLine();
        }


        static void Registr()
        {
            Options.SwitchRegister();
            bool registr = Options.Register;

            if (registr == true)
                Console.WriteLine("Регістр: чутливий");
            else
                Console.WriteLine("Регістр: ігнорувати");
        }

        public static void Run(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "//words":
                        if (i + 1 < args.Length)
                        {
                            string[] words = args[++i].Split(',');
                            Options.Words.AddRange(words);
                        }
                        break;
                 
                    case "//wordfile":
                        if (i + 1 < args.Length && File.Exists(args[i + 1]))
                            Options.LoadWordsFromFile(args[++i]);
                        break;
                    case "//search":
                        if (i + 1 < args.Length) Options.SetSearchPath(args[++i]);
                        break;
                    case "//output":
                        if (i + 1 < args.Length) Options.SetOutput(args[++i]);
                        break;
                    case "//ext":
                        if (i + 1 < args.Length) Options.SetExtensions(args[++i]);
                        break;
                    case "//case":
                        Options.Register = true;
                        break;
                }
            }

            if (Options.Words.Count == 0)
            {
                Console.WriteLine("Вкажіть слова: --words слово1,слово2");
                Prints.PrintUsage();
                return;
            }

            Options.EnsureOutputDefault();
            Scanner.StartScan();
        }
    }
}
