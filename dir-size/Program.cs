using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Humanizer;

namespace DirSize
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            CommandLineApplication.Execute<Program>(args);
        }

        [Option(Description = "The directory to calculate size", ShortName = "d")]
        public static string WorkingDirectory { get; set; }

        [Option(Description = "If sorted by size", ShortName = "s")]
        public static bool ShouldSort { get; set; }

        private void OnExecute()
        {
            try
            {
                if (string.IsNullOrEmpty(WorkingDirectory))
                {
                    WorkingDirectory = Directory.GetCurrentDirectory();
                }

                var nameSizeList = new List<(string name, long size)>();

                if (ShouldSort)
                {
                    Console.Write("\rScanning...");
                }

                Parallel.ForEach(
                    EnumerateTopLevelDirectories(WorkingDirectory),
                    () => new List<(string dirName, long size)>(),
                    (subDir, loopState, local) =>
                    {
                        var dirName = Path.GetFileName(subDir);
                        var size = CalculateDirSize(subDir);

                        if (!ShouldSort)
                            PrintSingle(dirName, size);

                        local.Add((dirName, size));
                        return local;
                    },
                    local =>
                    {
                        if (!local.Any()) return;

                        lock (nameSizeList)
                        {
                            nameSizeList.AddRange(local);
                        }
                    }
                );

                if (ShouldSort)
                {
                    nameSizeList.Sort((x, y) => y.size.CompareTo(x.size));
                    Console.Write("\r");
                    PrintAll(nameSizeList);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Unexpected error: " + ex);
                Console.ResetColor();
            }
        }

        private static IEnumerable<string> EnumerateTopLevelDirectories(string parentDir)
        {
            return new FileSystemEnumerable<string>(
                parentDir,
                (ref FileSystemEntry entry) => entry.ToFullPath(),
                new EnumerationOptions
                {
                    RecurseSubdirectories = false
                })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => entry.IsDirectory
            };
        }

        private static long CalculateDirSize(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return 0L;

                return new FileSystemEnumerable<long>(
                    dir,
                    (ref FileSystemEntry entry) => entry.Length,
                    new EnumerationOptions
                    {
                        RecurseSubdirectories = true
                    })
                {
                    ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory
                }.Sum();
            }
            catch
            {
                return -1;
            }
        }

        private static void PrintSingle(string dirName, long size)
        {
            Console.WriteLine(
                $"{dirName,-50} {(size >= 0 ? size.Bytes().ToString("#.#") : "N/A"),-10}");
        }

        private static void PrintAll(IEnumerable<(string dirName, long size)> list)
        {
            foreach (var (dirName, size) in list)
            {
                PrintSingle(dirName, size);
            }
        }
    }
}
