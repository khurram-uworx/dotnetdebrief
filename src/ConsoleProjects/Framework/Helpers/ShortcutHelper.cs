using IWshRuntimeLibrary;
using System;
using System.IO;

namespace Framework.Helpers
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Component_Object_Model
    /// https://en.wikipedia.org/wiki/Windows_Script_Host
    /// </summary>
    internal static class ShortcutHelper
    {
        public static string CreateDesktopShortcut(string targetPath, string shortcutName, string description = null)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutPath = Path.Combine(desktopPath, $"{shortcutName}.lnk");

            var shell = new WshShell();
            IWshShortcut shortcut = shell.CreateShortcut(shortcutPath);

            shortcut.Description = description ?? shortcutName;
            shortcut.TargetPath = targetPath;
            shortcut.Save();
            
            return shortcutPath;
        }
    }
}
