using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.ComponentModel;

namespace zuanke8
{
    public class AppSettings : INotifyPropertyChanged
    {
        private static readonly string ConfigPath = Path.Combine(
            Path.GetDirectoryName(Application.ResourceAssembly.Location) ?? "",
            "config.ini");

        private int _crawlFrequency = 30;
        public int CrawlFrequency
        {
            get => _crawlFrequency;
            set
            {
                if (_crawlFrequency != value)
                {
                    _crawlFrequency = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> Blacklist { get; set; } = new();
        public ObservableCollection<string> Highlights { get; set; } = new();
        public bool EnableNotification { get; set; } = true;
        public bool OnlyHighlightNotification { get; set; } = false;

        private bool _isTopMost;
        public bool IsTopMost
        {
            get => _isTopMost;
            set
            {
                if (_isTopMost != value)
                {
                    _isTopMost = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showFavoritesOnly;
        public bool ShowFavoritesOnly
        {
            get => _showFavoritesOnly;
            set
            {
                if (_showFavoritesOnly != value)
                {
                    _showFavoritesOnly = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _barkUrl;
        public string BarkUrl
        {
            get => _barkUrl;
            set
            {
                if (_barkUrl != value)
                {
                    _barkUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        private static AppSettings? _instance;
        public static AppSettings Instance => _instance ??= Load();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static AppSettings Load()
        {
            var settings = new AppSettings();
            if (!File.Exists(ConfigPath)) return settings;

            try
            {
                var lines = File.ReadAllLines(ConfigPath);
                string currentSection = "";

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Trim('[', ']');
                        continue;
                    }

                    var parts = line.Split('=');
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    switch (currentSection)
                    {
                        case "General":
                            if (key == "CrawlFrequency") settings.CrawlFrequency = int.Parse(value);
                            else if (key == "EnableNotification") settings.EnableNotification = bool.Parse(value);
                            else if (key == "OnlyHighlightNotification") settings.OnlyHighlightNotification = bool.Parse(value);
                            else if (key == "IsTopMost") settings.IsTopMost = bool.Parse(value);
                            else if (key == "BarkUrl") settings.BarkUrl = value;
                            break;
                        case "Blacklist":
                            settings.Blacklist.Add(value);
                            break;
                        case "Highlights":
                            settings.Highlights.Add(value);
                            break;
                    }
                }
            }
            catch
            {
                // 如果读取失败，使用默认设置
            }

            return settings;
        }

        public void Save()
        {
            try
            {
                using var writer = new StreamWriter(ConfigPath);
                
                // 保存通用设置
                writer.WriteLine("[General]");
                writer.WriteLine($"CrawlFrequency={CrawlFrequency}");
                writer.WriteLine($"EnableNotification={EnableNotification}");
                writer.WriteLine($"OnlyHighlightNotification={OnlyHighlightNotification}");
                writer.WriteLine($"IsTopMost={IsTopMost}");
                writer.WriteLine($"BarkUrl={BarkUrl}");

                // 保存黑名单
                writer.WriteLine("\n[Blacklist]");
                foreach (var item in Blacklist)
                {
                    writer.WriteLine($"Item={item}");
                }

                // 保存高亮列表
                writer.WriteLine("\n[Highlights]");
                foreach (var item in Highlights)
                {
                    writer.WriteLine($"Item={item}");
                }
            }
            catch
            {
                MessageBox.Show("保存设置失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 