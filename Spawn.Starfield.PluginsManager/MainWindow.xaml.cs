using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Spawn.Starfield.PluginsManager
{
    public partial class MainWindow : Window
    {
        private static Comparison<Item> s_comparison = new((a, b) =>
        {
            if (a.LoadIndex == -1 && b.LoadIndex == -1)
                return a.Name.CompareTo(b.Name);
            else if (a.LoadIndex != -1 && b.LoadIndex != -1)
                return a.LoadIndex.CompareTo(b.LoadIndex);
            else return a.LoadIndex == -1 ? 1 : -1;
        });

        private const string RESOURCE_TAG = "sResourceIndexFileList=";

        private string m_strPluginsFilePath;
        private string m_strStarfieldCustomIniFilePath;

        public MainWindow()
        {
            InitializeComponent();

            m_strPluginsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starfield", "plugins.txt");
            m_strStarfieldCustomIniFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Starfield", "StarfieldCustom.ini");

            UpdateTitle();
        }

        private void UpdateTitle()
        {
            if (!string.IsNullOrEmpty(App.DataDir))
            {
                FileInfo fi = new(Path.Combine(App.DataDir, "..", "Starfield.exe"));

                if (fi.Exists)
                {
                    try
                    {
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(fi.FullName);
                        Title += $" (Starfield {new Version(versionInfo.FileVersion!).ToString(3)})";
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PluginList.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("LoadIndex", System.ComponentModel.ListSortDirection.Ascending));
            ArchiveList.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("LoadIndex", System.ComponentModel.ListSortDirection.Ascending));

            LoadPlugins();

            LoadArchives();
        }

        #region Plugins
        private void LoadPlugins()
        {
            List<Item> lstDataPlugins = LoadPluginsFromDataDirectory();
            List<Item> lstLoadedPlugins = LoadPluginsFromFile(m_strPluginsFilePath);

            PluginList.ItemsSource = CompareAndSortPlugins(lstDataPlugins, lstLoadedPlugins);

            //WritePluginsToFile(m_strPluginsFilePath, false);

            PluginsUpButton.IsEnabled = false;
            PluginsDownButton.IsEnabled = false;
        }

        private List<Item> CompareAndSortPlugins(List<Item> dataPlugins, List<Item> loadedPlugins)
        {
            List<Item> lstPlugins = [];

            try
            {
                for (int i = 0; i < dataPlugins.Count; i++)
                {
                    Item? item = loadedPlugins.Where(p => dataPlugins[i].Name.Equals(p.Name)).FirstOrDefault();

                    if (item != null)
                        lstPlugins.Add(item);
                    else
                        lstPlugins.Add(dataPlugins[i]);
                }

                lstPlugins.Sort(s_comparison);

                for (int i = 0; i < lstPlugins.Count; i++)
                {
                    lstPlugins[i].LoadIndex = i;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't compare plugins!\r\n\r\n{ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return lstPlugins;
        }

        private void WritePluginsToFile(string filePath, bool showNotification)
        {
            try

            {
                StringBuilder sb = new();

                sb.AppendLine("# Created by Spawn SF Plugins Manager");

                for (int i = 0; i < PluginList.Items.Count; i++)
                {
                    Item item = (PluginList.Items[i] as Item)!;
                    string strLine = item.Name;

                    if (item.IsActive)
                        strLine = $"*{strLine}";

                    sb.AppendLine(strLine);
                }

                File.WriteAllText(filePath, sb.ToString());

                if (showNotification)
                    MessageBox.Show(this, "Saved plugins file successfuly.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't save plugins.txt file!\r\n\r\n{ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<Item> LoadPluginsFromFile(string filePath)
        {
            List<Item> lstRet = [];

            try
            {
                if (File.Exists(filePath))
                {
                    using FileStream fs = File.OpenRead(filePath);
                    using StreamReader sr = new(fs);

                    string? strLine = null;

                    while ((strLine = sr.ReadLine()) != null)
                    {
                        if (!strLine.StartsWith('#'))
                        {
                            bool blnIsActive = false;

                            if (strLine.StartsWith('*'))
                            {
                                strLine = strLine[1..];
                                blnIsActive = true;
                            }

                            Item item = new(strLine, blnIsActive)
                            {
                                LoadIndex = lstRet.Count
                            };

                            lstRet.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't load plugins!\r\n\r\n{ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return lstRet;
        }

        private List<Item> LoadPluginsFromDataDirectory()
        {
            List<Item> lstRet = [];

            if (!string.IsNullOrEmpty(App.DataDir))
            {
                string[] vFiles = Directory.GetFiles(App.DataDir, "*.esm").Select(f => Path.GetFileName(f)).ToArray();
                string[] vFilteredFiles = vFiles.Where(f => !AppSettings.Default.DefaultPlugins.Contains(f)).ToArray();

                for (int i = 0; i < vFilteredFiles.Length; i++)
                {
                    lstRet.Add(new Item(vFilteredFiles[i], false));
                }
            }

            return lstRet;
        }

        private void PluginsCancel_Click(object sender, RoutedEventArgs e)
        {
            LoadPlugins();
        }

        private void PluginsSave_Click(object sender, RoutedEventArgs e)
        {
            WritePluginsToFile(m_strPluginsFilePath, true);
        }

        private void PluginsUpButton_Click(object sender, RoutedEventArgs e)
        {
            DecreaseLoadIndex(PluginList, PluginsUpButton, PluginsDownButton);
        }

        private void PluginsDownButton_Click(object sender, RoutedEventArgs e)
        {
            IncreaseLoadIndex(PluginList, PluginsUpButton, PluginsDownButton);
        }

        private void PluginList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsState(PluginList, PluginsUpButton, PluginsDownButton);
        }
        #endregion

        #region Archives
        private void LoadArchives()
        {
            List<Item> lstDataArchives = LoadArchivesFromDataDirectory();
            List<Item> lstLoadedArchives = LoadArchivesFromFile(m_strStarfieldCustomIniFilePath);

            ArchiveList.ItemsSource = CompareAndSortArchives(lstDataArchives, lstLoadedArchives);

            ArchivesUpButton.IsEnabled = false;
            ArchivesDownButton.IsEnabled = false;
        }

        private List<Item> LoadArchivesFromDataDirectory()
        {
            List<Item> lstRet = [];

            if (!string.IsNullOrEmpty(App.DataDir))
            {
                string[] vFiles = Directory.GetFiles(App.DataDir, "*.ba2").Select(f => Path.GetFileName(f)).ToArray();
                string[] vFilteredFiles = vFiles.Where(f => !AppSettings.Default.DefaultArchives.Contains(f)).ToArray();

                for (int i = 0; i < vFilteredFiles.Length; i++)
                {
                    lstRet.Add(new Item(vFilteredFiles[i], false));
                }
            }

            return lstRet;
        }

        private List<Item> LoadArchivesFromFile(string filePath)
        {
            List<Item> lstRet = [];

            try
            {
                if (File.Exists(filePath))
                {
                    using FileStream fs = File.OpenRead(filePath);
                    using StreamReader sr = new(fs);

                    string? strLine = null;

                    string strResourceKey = "sResourceIndexFileList=";

                    while ((strLine = sr.ReadLine()) != null)
                    {
                        if (strLine.StartsWith(strResourceKey))
                        {
                            strLine = strLine.Replace(strResourceKey, string.Empty);

                            string[] vArchives = strLine.Split(',');

                            for (int i = 0; i < vArchives.Length; i++)
                            {
                                string strArchive = vArchives[i].Trim();
                                if (!AppSettings.Default.DefaultArchives.Contains(strArchive))
                                {
                                    Item item = new(strArchive, true)
                                    {
                                        LoadIndex = lstRet.Count
                                    };

                                    lstRet.Add(item);
                                }
                            }

                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't load archives from ini file!\r\n\r\n{ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return lstRet;
        }

        private List<Item> CompareAndSortArchives(List<Item> dataArchives, List<Item> loadedArchives)
        {
            List<Item> lstArchives = [];

            try
            {
                for (int i = 0; i < dataArchives.Count; i++)
                {
                    Item? item = loadedArchives.Where(p => dataArchives[i].Name.Equals(p.Name)).FirstOrDefault();

                    if (item != null)
                        lstArchives.Add(item);
                    else
                        lstArchives.Add(dataArchives[i]);
                }

                lstArchives.Sort(s_comparison);

                for (int i = 0; i < lstArchives.Count; i++)
                {
                    lstArchives[i].LoadIndex = i;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't compare archives!\r\n\r\n{ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return lstArchives;
        }

        private void ArchiveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsState(ArchiveList, ArchivesUpButton, ArchivesDownButton);
        }

        private void ArchivesUpButton_Click(object sender, RoutedEventArgs e)
        {
            DecreaseLoadIndex(ArchiveList, ArchivesUpButton, ArchivesDownButton);
        }

        private void ArchivesDownButton_Click(object sender, RoutedEventArgs e)
        {
            IncreaseLoadIndex(ArchiveList, ArchivesUpButton, ArchivesDownButton);
        }

        private string AssembleResourceString()
        {
            StringBuilder sb = new();

            sb.Append(RESOURCE_TAG);
            sb.Append("Starfield - LODTextures.ba2, ");
            sb.Append("Starfield - Textures01.ba2, ");
            sb.Append("Starfield - Textures02.ba2, ");
            sb.Append("Starfield - Textures03.ba2, ");
            sb.Append("Starfield - Textures04.ba2, ");
            sb.Append("Starfield - Textures05.ba2, ");
            sb.Append("Starfield - Textures06.ba2, ");
            sb.Append("Starfield - Textures07.ba2, ");
            sb.Append("Starfield - Textures08.ba2, ");
            sb.Append("Starfield - Textures09.ba2, ");
            sb.Append("Starfield - Textures10.ba2, ");
            sb.Append("Starfield - Textures11.ba2, ");
            sb.Append("Starfield - TexturesPatch.ba2");

            try
            {
                List<Item> lstArchives = ArchiveList.Items.Cast<Item>().Where(i => i.IsActive).ToList();

                StringBuilder sbCustomArchives = new();

                for (int i = 0; i < lstArchives.Count; i++)
                {
                    sbCustomArchives.Append($", {lstArchives[i].Name}");
                }

                sb.Append(sbCustomArchives);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't create resource string!\r\n\r\n{ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return sb.ToString();
        }

        private void CopyToClipboard(string value)
        {
            try
            {
                Clipboard.SetText(value);

                MessageBox.Show("Copied resource string to clipboard. Paste it into your StarfieldCustom.ini file.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't copy resource string to clipboard!\r\n\r\n{ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WriteToCustomIniFile(string filePath, string resourceString)
        {
            string? strContent = TextFileManager.ReadStringFromFile(filePath);

            if (!string.IsNullOrEmpty(strContent))
            {
                string[] vCurrentContent = strContent.Split("\r\n");

                int nIndex = Array.FindIndex(vCurrentContent, s => s.StartsWith(RESOURCE_TAG));
                vCurrentContent[nIndex] = resourceString;

                TextFileManager.WriteStringToFile(string.Join("\r\n", vCurrentContent), filePath);

                MessageBox.Show("Replaced resource string in the StarfieldCustom.ini file.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ArchivesSave_Click(object sender, RoutedEventArgs e)
        {
            string strResourceString = AssembleResourceString();

            WriteToCustomIniFile(m_strStarfieldCustomIniFilePath, strResourceString);
        }

        private void ArchivesCopy_Click(object sender, RoutedEventArgs e)
        {
            string strResourceString = AssembleResourceString();

            CopyToClipboard(strResourceString);
        }

        private void ArchivesCancel_Click(object sender, RoutedEventArgs e)
        {
            LoadArchives();
        }
        #endregion

        private void UpdateButtonsState(ListView listView, Button upButton, Button downButton)
        {
            if (listView.SelectedItems.Count > 0)
            {
                upButton.IsEnabled = (listView.SelectedItems[0] as Item)!.LoadIndex > 0;
                downButton.IsEnabled = (listView.SelectedItems[listView.SelectedItems.Count - 1] as Item)!.LoadIndex <= listView.Items.Count - 2;
            }
            else
            {
                upButton.IsEnabled = false;
                downButton.IsEnabled = false;
            }
        }

        private void DecreaseLoadIndex(ListView listView, Button upButton, Button downButton)
        {
            if (listView.SelectedItem is Item item && item.LoadIndex > 0)
            {
                List<Item> currentItems = listView.Items.Cast<Item>().ToList();
                Item upperItem = (listView.Items[item.LoadIndex - 1] as Item)!;

                upperItem.LoadIndex += 1;
                item.LoadIndex -= 1;

                currentItems[item.LoadIndex] = item;
                currentItems[upperItem.LoadIndex] = upperItem;

                listView.ItemsSource = currentItems;
                listView.UpdateLayout();

                UpdateButtonsState(listView, upButton, downButton);
            }
        }

        private void IncreaseLoadIndex(ListView listView, Button upButton, Button downButton)
        {
            if (listView.SelectedItem is Item item && item.LoadIndex < listView.Items.Count - 1)
            {
                List<Item> currentItems = listView.ItemsSource.Cast<Item>().ToList();
                Item lowerItem = (listView.Items[item.LoadIndex + 1] as Item)!;

                lowerItem.LoadIndex -= 1;
                item.LoadIndex += 1;

                currentItems[item.LoadIndex] = item;
                currentItems[lowerItem.LoadIndex] = lowerItem;

                listView.ItemsSource = currentItems;
                listView.UpdateLayout();

                UpdateButtonsState(listView, upButton, downButton);
            }
        }

        private void ListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                MouseWheelEventArgs eventArg = new(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = MouseWheelEvent,
                    Source = sender
                };
                UIElement? parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }

        private void SetDataDirectory_Click(object sender, RoutedEventArgs e)
        {
            App.SelectDataDirectory();

            MessageBox.Show("Updated Starfield data directory.", Title, MessageBoxButton.OK, MessageBoxImage.Information);

            LoadPlugins();
            LoadArchives();
        }
    }
}