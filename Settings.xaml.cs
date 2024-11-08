using System.Windows;
using System.Windows.Input;

namespace zuanke8
{
    public partial class Settings : Window
    {
        private readonly AppSettings _settings;

        public Settings()
        {
            InitializeComponent();
            _settings = AppSettings.Instance;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 加载设置
            FrequencySlider.Value = _settings.CrawlFrequency;
            BlacklistView.ItemsSource = _settings.Blacklist;
            HighlightView.ItemsSource = _settings.Highlights;
            EnableNotificationCheckBox.IsChecked = _settings.EnableNotification;
            OnlyHighlightNotificationCheckBox.IsChecked = _settings.OnlyHighlightNotification;
            BarkUrlInput.Text = _settings.BarkUrl;

            // 添加输入框回车事件
            BlacklistInput.KeyDown += (s, e) => 
            {
                if (e.Key == Key.Enter)
                {
                    AddBlacklist_Click(s, e);
                }
            };

            HighlightInput.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    AddHighlight_Click(s, e);
                }
            };
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddBlacklist_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BlacklistInput.Text)) return;

            if (!_settings.Blacklist.Contains(BlacklistInput.Text))
            {
                _settings.Blacklist.Add(BlacklistInput.Text);
                
                // 通知主窗口刷新显示
                if (Owner is MainWindow mainWindow)
                {
                    mainWindow.ApplySettings();
                }
            }
            BlacklistInput.Clear();
        }

        private void RemoveBlacklistItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is string item)
            {
                _settings.Blacklist.Remove(item);
                
                // 通知主窗口重新加载并应用设置
                if (Owner is MainWindow mainWindow)
                {
                    mainWindow.Posts.Clear();
                    mainWindow._currentPage = 0;
                    mainWindow.LoadPage(0);
                    mainWindow.ApplySettings();
                }
            }
        }

        private void AddHighlight_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HighlightInput.Text)) return;

            if (!_settings.Highlights.Contains(HighlightInput.Text))
            {
                _settings.Highlights.Add(HighlightInput.Text);
            }
            HighlightInput.Clear();
        }

        private void RemoveHighlightItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is string item)
            {
                _settings.Highlights.Remove(item);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 保存设置
            _settings.CrawlFrequency = (int)FrequencySlider.Value;
            _settings.EnableNotification = EnableNotificationCheckBox.IsChecked ?? false;
            _settings.OnlyHighlightNotification = OnlyHighlightNotificationCheckBox.IsChecked ?? false;
            _settings.BarkUrl = BarkUrlInput.Text?.Trim();
            _settings.Save();

            MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
} 