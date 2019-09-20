using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private void OnExecute()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrEmpty(WorkingDirectory))
                {
                    WorkingDirectory = Directory.GetCurrentDirectory();
                }

                Parallel.ForEach(EnumerateTopLevelDirectories(WorkingDirectory),
                    subDir => { Print(subDir, CalculateDirSize(subDir)); }
                );

                var elapsed = sw.ElapsedMilliseconds;
                Console.Out.WriteLine($"---------- Finished in {elapsed} ms ----------");
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

        private static void Print(string dir, long size)
        {
            var dirName = Path.GetFileName(dir);
            Console.WriteLine(
                $"{dirName,-50} {(size >= 0 ? size.Bytes().ToString("#.#") : "N/A"),-10}");
        }
    }
}
