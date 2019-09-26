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

        [Option(Description = "The directory to working with", ShortName = "d")]
        public static string WorkingDir { get; set; }

        [Option(Description = "Sort by size", ShortName = "s")]
        public static bool Sort { get; set; }

        private void OnExecute()
        {
            try
            {
                if (string.IsNullOrEmpty(WorkingDir))
                {
                    WorkingDir = Directory.GetCurrentDirectory();
                }

                var nameSizeList = new List<(string name, long size)>();

                if (Sort)
                {
                    Console.Write("\rScanning...");
                }

                Parallel.ForEach(
                    EnumerateTopLevelDirectories(WorkingDir),
                    () => new List<(string dirName, long size)>(),
                    (subDir, loopState, local) =>
                    {
                        var dirName = Path.GetFileName(subDir);
                        var size = CalculateDirSize(subDir);

                        if (!Sort)
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

                if (Sort)
                {
                    nameSizeList.Sort((x, y) => x.size.CompareTo(y.size));
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
                    RecurseSubdirectories = false,
                    AttributesToSkip = FileAttributes.ReparsePoint, //Avoiding infinite loop
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.PlatformDefault,
                    ReturnSpecialDirectories = false
                })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => entry.IsDirectory
            };
        }

        private static long CalculateDirSize(string dir)
        {
            if (!Directory.Exists(dir)) return 0L;

            // https://blogs.msdn.microsoft.com/jeremykuhne/2018/03/09/custom-directory-enumeration-in-net-core-2-1/
            return new FileSystemEnumerable<long>(
                dir,
                (ref FileSystemEntry entry) => entry.Length,
                new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    AttributesToSkip = FileAttributes.ReparsePoint, //Avoiding infinite loop
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.PlatformDefault,
                    ReturnSpecialDirectories = false
                })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory
            }.Sum();
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
