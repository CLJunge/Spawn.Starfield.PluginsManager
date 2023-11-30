using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace Spawn.Starfield.PluginsManager
{
    public partial class App : Application
    {
        private const string DATA_DIR_FILE = "data_dir.txt";
        public static string? DataDir { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

#if DEBUG
            //TextFileManager.WriteStringToFile("D:\\SteamLibrary\\steamapps\\common\\Starfield\\Data", DATA_DIR_FILE);
#endif

            DataDir = TextFileManager.ReadStringFromFile(DATA_DIR_FILE);

            if (string.IsNullOrEmpty(DataDir))
            {
                MessageBox.Show("The Starfield Data directory has not been set!", "Spawn | Plugins Manager", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                SelectDataDirectory();
            }
        }

        public static void SelectDataDirectory()
        {
            string strDataDir = ShowFileDialog();

            if (!Directory.Exists(strDataDir))
            {
                MessageBox.Show("The selected directory does not exist!", "Invalid data directory!", MessageBoxButton.OK, MessageBoxImage.Error);

                SelectDataDirectory();
            }
            else
            {
                DataDir = strDataDir;
                TextFileManager.WriteStringToFile(strDataDir, DATA_DIR_FILE);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unexpected error occured!\r\n\r\n{e.ExceptionObject}\r\n\r\nThe app is going to exit.");

            Environment.Exit(-1);
        }

        public static string ShowFileDialog()
        {
            string strRet = string.Empty;

            OpenFolderDialog ofd = new()
            {
                Title = "Select Starfield Data directory...",
                Multiselect = false
            };

            bool? blnResult = ofd.ShowDialog();

            if (blnResult.HasValue && blnResult.Value && !string.IsNullOrWhiteSpace(ofd.FolderName))
            {
                strRet = ofd.FolderName;
            }

            return strRet;
        }
    }
}