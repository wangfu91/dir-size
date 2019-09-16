using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Humanizer;
using Humanizer.Bytes;

namespace FolderSize
{
    class Program
    {
        public static void Main(string[] args)
        {
            CommandLineApplication.Execute<Program>(args);
        }

        [Option(Description = "The directory to calculate size", ShortName = "d")]
        public string WorkingDirectory { get; set; }

        private void OnExecute()
        {
            try
            {
                if (string.IsNullOrEmpty(WorkingDirectory))
                {
                    WorkingDirectory = Directory.GetCurrentDirectory();
                }

                foreach (var childDir in Directory.GetDirectories(WorkingDirectory))
                {
                    PrintDirSizeInfo(childDir, CalculateDirSize(childDir));
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Unexpected error: " + ex.ToString());
                Console.ResetColor();
            }
        }

        private long CalculateDirSize(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return 0L;

                var dirSize = 0L;
                foreach (var file in Directory.GetFiles(dir, "*"))
                {
                    dirSize += new FileInfo(file).Length;
                }

                return dirSize;
            }
            catch
            {
                return -1;
            }
        }

        private void PrintDirSizeInfo(string dir, long size)
        {
            var dirName = Path.GetFileName(dir);
            Console.WriteLine("{0,-30} {1,5}", dirName,
            size > 0
            ? size.Bytes().ToString("#.#")
            : "N/A");
        }
    }
}
