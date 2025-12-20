using System;
using System.Collections.Generic;
using System.Text;

namespace Tools;

public static class FileTools
{
    public static string ListFiles(string path)
    {
        return string.Join("\n", Directory.GetFiles(path));
    }

    public static string ReadFile(string file)
    {
        return File.ReadAllText(file);
    }
}
