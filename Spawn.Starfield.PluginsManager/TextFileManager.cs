using System.IO;

namespace Spawn.Starfield.PluginsManager
{
    public static class TextFileManager
    {
        public static void WriteStringToFile(string value, string filePath)
        {
            File.WriteAllText(filePath, value);
        }

        public static string? ReadStringFromFile(string filePath)
        {
            string? strRet = null;

            if (File.Exists(filePath))
                strRet = File.ReadAllText(filePath);

            return strRet;
        }
    }
}