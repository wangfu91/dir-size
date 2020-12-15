using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Humanizer;
using System.Threading;

namespace DirSize
{
    [Command(
        Name = "dir-size",
        Description = "A simple dotnet global tool to calculate the size of each sub-dir.")]
    [HelpOption("-?|-h|--help")]
    [VersionOptionFromMember(Template = "-v|--version", MemberName = nameof(GetVersion))]
    internal class Program
    {
        private static readonly IReporter Reporter = new ConsoleReporter(PhysicalConsole.Singleton);
        private static readonly CancellationTokenSource cts = new();

        [Option("-d|--dir", Description = "The directory to work with, default is the current dir")]
        public static string WorkingDir { get; set; } = Directory.GetCurrentDirectory();

        [Option("-s|--sort", Description = "Sort by size")]
        public static bool Sort { get; set; }

        public static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            CommandLineApplication.Execute<Program>(args);
        }

        private void OnExecute()
        {
            try
            {
                var nameSizeList = new List<(string name, long size)>();

                if (Sort)
                {
                    Console.Write("Scanning...");
                }

                var options = new ParallelOptions
                {
                    CancellationToken = cts.Token,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                Parallel.ForEach(
                    EnumerateTopLevelDirectories(WorkingDir),
                    options,
                    () => new List<(string dirName, long size)>(),
                    (subDir, _, local) =>
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
            catch (OperationCanceledException ex)
            {
                Reporter.Warn(Environment.NewLine + ex.Message);
            }
            catch (Exception ex)
            {
                Reporter.Error("Unexpected error: " + ex);
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
            Reporter.Output($"{dirName,-50} {(size >= 0 ? size.Bytes().ToString("#.#") : "N/A"),-10}");
        }

        private static void PrintAll(IEnumerable<(string dirName, long size)> list)
        {
            foreach (var (dirName, size) in list)
            {
                PrintSingle(dirName, size);
            }
        }

        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        }

        private static string? GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }
    }
}
