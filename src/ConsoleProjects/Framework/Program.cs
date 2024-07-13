using Framework.Helpers;
using System;
using System.Diagnostics;
using System.IO;

namespace Framework
{
    internal class Program
    {
        static void exit(int code, string print = null)
        {
            if (!string.IsNullOrEmpty(print))
                Console.WriteLine($"Exiting with {code}, {print}");
            else
                Console.WriteLine($"Exiting with {code}");

            Environment.Exit(code);
        }

        static void Main(string[] args)
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appPath = Path.Combine(localAppDataPath, "DotNet Debrief");

            if (!Directory.Exists(appPath)) Directory.CreateDirectory(appPath);
            var destinationFile = Path.Combine(appPath, "app.exe");

            if (File.Exists(destinationFile))
            {
                Console.WriteLine($"Deleting {destinationFile}");
                File.Delete(destinationFile);
            }
            File.Copy("app.exe", destinationFile);

            if (!File.Exists(destinationFile))
                exit(1, "Failed to copy file");
            else
                Console.WriteLine($"File is copied to {destinationFile}");

            var shortcutPath = ShortcutHelper.CreateDesktopShortcut(destinationFile, "DotNet Debrief");

            Console.WriteLine("Desktop shortcut is created, launching it");
            Process.Start(shortcutPath);

            Console.WriteLine("Press Enter to quit");
            Console.ReadLine();

            exit(0);
        }
    }
}
