using Microsoft.Win32;
using System.Windows;

namespace Spawn.Starfield.PluginsManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

#if DEBUG
            AppSettings.Default.DataDirectory = "D:\\SteamLibrary\\steamapps\\common\\Starfield\\Data";
            AppSettings.Default.Save();
#endif

            if (string.IsNullOrEmpty(AppSettings.Default.DataDirectory))
            {
                MessageBox.Show("The Starfield Data directory has not been set!", "Spawn | Plugins Manager", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                AppSettings.Default.DataDirectory = SelectDataDirectory();
                AppSettings.Default.Save();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unexpected error occured!\r\n\r\n{e.ExceptionObject}\r\n\r\nThe app is going to exit.");

            Environment.Exit(-1);
        }

        private static string SelectDataDirectory()
        {
            string strRet = string.Empty;

            OpenFolderDialog ofd = new()
            {
                Title = "Select Starfield Data directory...",
                Multiselect = false
            };

            bool? blnResult = ofd.ShowDialog();

            if (blnResult.HasValue && blnResult.Value)
            {
                strRet = ofd.FolderName;
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Please select the Data directory to continue!", "Spawn | Plugins Manager", MessageBoxButton.OKCancel, MessageBoxImage.Hand);

                if (result == MessageBoxResult.OK)
                {
                    strRet = SelectDataDirectory();
                }
                else
                {
                    Environment.Exit(0);
                }
            }

            return strRet;
        }
    }
}