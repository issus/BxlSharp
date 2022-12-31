using System;
using System.IO;
using System.Linq;
using OriginalCircuit.BxlSharp;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = args[0];
            var files = Enumerable.Concat(
                Directory.GetFiles(path, "*.bxl"),
                Directory.GetFiles(path, "*.xlr")).ToList();
            var n = 0;
            foreach (var filename in files)
            {
                Console.WriteLine($"{++n}/{files.Count} {Path.GetFileName(filename)}");
                if (Path.GetExtension(filename) == ".bxl")
                {
                    if (!File.Exists($"{filename}.txt"))
                    {
                        var xlrdata = BxlDocument.DecodeBxl(File.ReadAllBytes(filename));
                        File.WriteAllText($"{filename}.txt", xlrdata);
                    }
                }

                var data = BxlDocument.ReadFromFile(filename, BxlFileType.FromExtension, out var logs);
                foreach (var entry in logs)
                {
                    switch (entry.Severity)
                    {
                        case LogSeverity.Information:
                            Console.ForegroundColor = ConsoleColor.Blue;
                            break;
                        case LogSeverity.Warning:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case LogSeverity.Error:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                    }
                    //if (entry.Severity != LogSeverity.Information)
                    {
                        Console.WriteLine(entry.Message);
                    }
                }
                Console.ResetColor();
                Console.WriteLine();

                if (logs.Any(e => e.IsError))
                {
                    Console.WriteLine("Error, press any key to continue");
                    Console.ReadKey();
                }
                //var json = Newtonsoft.Json.Linq.JObject.FromObject(data).ToString();
                //Console.WriteLine(json);

            }

            Console.WriteLine("DONE");
            Console.ReadKey();
        }
    }
}
