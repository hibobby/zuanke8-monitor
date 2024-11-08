﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using zuanke8.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;

namespace zuanke8
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly AppSettings _settings;
        private readonly PostManager _postManager;
        private bool _orderByLastReply = true;
        public ObservableCollection<PostViewModel> Posts { get; set; }
        private readonly ForumCrawler _crawler;

        private int _unreadCount;
        public int UnreadCount
        {
            get => _unreadCount;
            set
            {
                if (_unreadCount != value)
                {
                    _unreadCount = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
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
                    ApplySettings();
                }
            }
        }

        private bool _isLoading;
        internal int _currentPage;
        private const int PageSize = 20;

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _lastVerticalOffset;
        private const double PullToRefreshThreshold = -50;

        private double _pendingScrollOffset = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _settings = AppSettings.Instance;
            _postManager = new PostManager();
            _currentPage = 0;
            
            // 初始化 Posts 集合
            Posts = new ObservableCollection<PostViewModel>();
            PostsListView.ItemsSource = Posts;
            
            _orderByLastReply = false;  // 默认按发帖时间排序
            _crawler = new ForumCrawler(UpdatePosts, _postManager);
            
            // 监听设置变化
            _settings.Blacklist.CollectionChanged += (s, e) => ApplySettings();
            _settings.Highlights.CollectionChanged += (s, e) => ApplySettings();
            _settings.PropertyChanged += Settings_PropertyChanged;

            // 加载已有帖子并应用设置
            LoadPage(_currentPage);
            ApplySettings();  // 确保应用设置
            UpdateCounts();

            // 检查登录状态并启动定时刷新
            if (CookieManager.CheckLoginStatus())
            {
                _crawler.Start(_settings.CrawlFrequency);
            }
            else
            {
                MessageBox.Show("请先登录！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // 更新序按钮提示
            UpdateSortButtonTooltip();

            // 应用置顶状态
            this.Topmost = _settings.IsTopMost;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSettings.CrawlFrequency))
            {
                // 更新爬取频率
                if (CookieManager.CheckLoginStatus())
                {
                    _crawler.Start(_settings.CrawlFrequency);
                }
            }
        }

        public void ApplySettings()
        {
            // 保存当前滚动位置
            var scrollViewer = GetScrollViewer(PostsListView);
            var currentOffset = scrollViewer?.VerticalOffset ?? 0;

            // 获取当前显示的帖子
            var currentPosts = Posts.ToList();
            var filteredPosts = currentPosts.Where(p => 
                !_settings.Blacklist.Any(keyword => 
                    p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) || 
                    p.Author.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .Where(p => !ShowFavoritesOnly || p.IsFavorite)
                .ToList();

            // 更新高亮状态
            foreach (var post in filteredPosts)
            {
                post.IsHighlight = _settings.Highlights.Any(keyword =>
                    post.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    post.Author.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            // 如果过滤后的帖子数量与当前显示的不同，才需要更新列表
            if (filteredPosts.Count != Posts.Count || 
                !filteredPosts.SequenceEqual(Posts))
            {
                Posts.Clear();
                foreach (var post in filteredPosts)
                {
                    Posts.Add(post);
                }
            }

            UpdateCounts();
            Debug.WriteLine($"应用设置后：显示 {Posts.Count} 个帖子，未读 {UnreadCount}，总数 {TotalCount}");

            // 恢复滚动位置
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(currentOffset);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (CookieManager.CheckLoginStatus())
            {
                if (MessageBox.Show("是否退出登录？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    CookieManager.ClearCookie();
                    _crawler.Stop();
                    UpdateLoginStatus();
                    MessageBox.Show("已退出登录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                var loginWindow = new Login
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                loginWindow.ShowDialog();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Settings
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            settingsWindow.ShowDialog();
            
            // 设置窗口关闭后重新应用设置
            ApplySettings();
        }

        private void UpdateLoginStatus()
        {
            bool isLoggedIn = CookieManager.CheckLoginStatus();
            var loginButton = this.FindName("LoginButton") as Button;
            if (loginButton != null)
            {
                loginButton.Content = isLoggedIn ? "\uE77B" : "\uE77B";
                loginButton.ToolTip = isLoggedIn ? "已登录" : "未登录";
                loginButton.Foreground = isLoggedIn ? 
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C4DFF")) :
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b2bec3"));
            }
        }

        private void UpdatePosts(ObservableCollection<Post> newPosts)
        {
            if (newPosts == null || newPosts.Count == 0)
            {
                IsRefreshing = false;
                return;
            }

            try
            {
                Debug.WriteLine($"收到新帖子：{newPosts.Count} 个");

                // 检查是否有新帖子
                var oldPosts = _postManager.GetAllPosts();
                var oldPostIds = oldPosts.Select(p => p.PostId).ToHashSet();

                var hasNewPosts = newPosts.Any(p => !oldPostIds.Contains(p.PostId));
                Debug.WriteLine($"是否有新帖子：{hasNewPosts}，当前帖子总数：{oldPosts.Count}");

                // 更新 PostManager
                _postManager.AddPosts(newPosts);

                // 如果有新帖子，才重新加载列表
                if (hasNewPosts)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Posts.Clear();
                        _currentPage = 0;
                        LoadPage(_currentPage);
                    });
                }
                else
                {
                    // 只更新现有帖子的属性
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var post in newPosts)
                        {
                            var existingPost = Posts.FirstOrDefault(p => p.PostId == post.PostId);
                            if (existingPost != null)
                            {
                                existingPost.LastReplyTime = post.LastReplyTime;
                                existingPost.LastReplyUser = post.LastReplyUser;
                                existingPost.ReplyCount = post.ReplyCount;
                                existingPost.ViewCount = post.ViewCount;
                            }
                        }

                        // 如果是按最后回复时间排序，需要重新排序
                        if (_orderByLastReply && newPosts.Where(x=>!x.IsHidden).Max(x=>x.LastReplyTime) > _postManager.GetPosts(_orderByLastReply).Max(x=>x.LastReplyTime))
                        {
                            var sortedPosts = Posts.OrderByDescending(p => p.LastReplyTime).ToList();
                            Posts.Clear();
                            foreach (var post in sortedPosts)
                            {
                                Posts.Add(post);
                            }
                        }
                    });
                }

                UpdateCounts();
                Debug.WriteLine($"更新显示：{Posts.Count} 个帖子，未读 {UnreadCount}，总数 {TotalCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update posts error: {ex.Message}");
                MessageBox.Show($"更新帖子失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        public void OnLoginSuccess()
        {
            // 更新登录状态
            UpdateLoginStatus();
            
            // 登录成功后立即刷新一次
            if (!_isRefreshing)
            {
                IsRefreshing = true;
                _ = _crawler.FetchPosts();
            }
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            _orderByLastReply = !_orderByLastReply;
            _currentPage = 0;
            Posts.Clear();
            LoadPage(_currentPage);
            ApplySettings();
            UpdateSortButtonTooltip();
        }

        private void UpdateSortButtonTooltip()
        {
            SortButton.ToolTip = _orderByLastReply ? "最回复时间排序" : "按发帖时间排序";
        }

        private void PostsListView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element && 
                element.DataContext is PostViewModel post)
            {
                try
                {
                    // 在默认浏览器中打开链接
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = post.Url,
                        UseShellExecute = true
                    });

                    // 标记为已读
                    post.IsRead = true;
                    _postManager.UpdatePost(new Post
                    {
                        PostId = post.PostId,
                        IsRead = true,
                        IsHidden = post.IsHidden,
                        IsFavorite = post.IsFavorite
                    });

                    // 刷新界面
                    PostsListView.Items.Refresh();
                    UpdateCounts();  // 更新未读计数
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开链接失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateCounts()
        {
            var allPosts = _postManager.GetPosts(_orderByLastReply);
            var visiblePosts = allPosts.AsEnumerable();

            // 根据当前模式决定过滤条件
            if (ShowFavoritesOnly)
            {
                // 收藏模式：只过滤收藏的帖子（包括隐藏的）
                visiblePosts = visiblePosts.Where(p => p.IsFavorite);
            }
            else
            {
                // 普通模式：过滤掉隐藏的帖子
                visiblePosts = visiblePosts.Where(p => !p.IsHidden);
            }

            // 应用黑名单过滤
            visiblePosts = visiblePosts.Where(p => !_settings.Blacklist.Any(keyword => 
                p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) || 
                p.Author.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

            UnreadCount = visiblePosts.Count(p => !p.IsRead);
            TotalCount = visiblePosts.Count();
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PostViewModel post)
            {
                // 保存当前滚动位置
                var scrollViewer = GetScrollViewer(PostsListView);
                var currentOffset = scrollViewer?.VerticalOffset ?? 0;

                post.IsFavorite = !post.IsFavorite;
                _postManager.UpdatePost(new Post
                {
                    PostId = post.PostId,
                    IsRead = post.IsRead,
                    IsHidden = post.IsHidden,
                    IsFavorite = post.IsFavorite
                });

                // 如果在收藏模式下，需要重新应用过滤
                if (ShowFavoritesOnly)
                {
                    ApplySettings();
                }

                // 恢复滚动位置
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(currentOffset);
                }
            }
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PostViewModel post)
            {
                // 保存当前滚动位置
                var scrollViewer = GetScrollViewer(PostsListView);
                var currentOffset = scrollViewer?.VerticalOffset ?? 0;

                // 更新帖子状态
                post.IsHidden = true;
                _postManager.UpdatePost(new Post
                {
                    PostId = post.PostId,
                    IsRead = post.IsRead,
                    IsHidden = true,
                    IsFavorite = post.IsFavorite
                });

                // 从当前显示列表中移除该帖子
                Posts.Remove(post);
                
                // 更新计数
                UpdateCounts();

                // 恢复滚动位置
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(currentOffset);
                }
            }
        }

        private void FavoriteFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ShowFavoritesOnly = !ShowFavoritesOnly;
            // 重新加载数据
            _currentPage = 0;
            Posts.Clear();
            LoadPage(_currentPage);
            UpdateCounts();
        }

        internal void LoadPage(int pageIndex)
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var allPosts = _postManager.GetPosts(_orderByLastReply);
                var query = allPosts.AsEnumerable();

                // 修改过滤逻辑
                if (ShowFavoritesOnly)
                {
                    // 收藏模式：只显示收藏的帖子（包括隐藏的）
                    query = query.Where(p => p.IsFavorite);
                }
                else
                {
                    // 普通模式：过滤掉隐藏的帖子
                    query = query.Where(p => !p.IsHidden);
                }

                // 应用黑名单过滤
                query = query.Where(p => !_settings.Blacklist.Any(keyword => 
                    p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) || 
                    p.Author.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

                // 转换为 ViewModels 并应用高亮
                var filteredPosts = query.Select(p => 
                {
                    var viewModel = new PostViewModel(p);
                    viewModel.IsHighlight = _settings.Highlights.Any(keyword =>
                        p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        p.Author.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                    return viewModel;
                });

                var pagedPosts = filteredPosts.Skip(pageIndex * PageSize).Take(PageSize);
                foreach (var post in pagedPosts)
                {
                    Posts.Add(post);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load page error: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void PostsListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = e.OriginalSource as ScrollViewer;
            if (scrollViewer == null) return;

            // 无限滚动
            if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 100)
            {
                var totalPages = (_postManager.GetPosts(_orderByLastReply).Count + PageSize - 1) / PageSize;
                if (!_isLoading && _currentPage < totalPages - 1)
                {
                    _currentPage++;
                    LoadPage(_currentPage);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("点击刷新按钮");
            if (!_isRefreshing)
            {
                if (CookieManager.CheckLoginStatus())
                {
                    IsRefreshing = true;
                    _ = _crawler.FetchPosts();  // 直接调用 ForumCrawler 的爬取方法
                }
                else
                {
                    MessageBox.Show("请先登录！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                Debug.WriteLine("正在刷新中，忽略点击");
            }
        }

        private void TopMostButton_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = !this.Topmost;
            _settings.IsTopMost = this.Topmost;  // 保存状态
            _settings.Save();  // 立即保存到文件
            TopMostButton.ToolTip = this.Topmost ? "取消置顶" : "窗口置顶";
        }

        // 添加获取 ScrollViewer 的辅助方法
        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer scrollViewer)
                return scrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private async Task RestoreScrollPosition(ScrollViewer scrollViewer, double position)
        {
            if (scrollViewer == null) return;

            // 等待布局更新
            await Task.Delay(100);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                scrollViewer.ScrollToVerticalOffset(position);
                Debug.WriteLine($"尝试恢复滚动位置到：{position}，当前位置：{scrollViewer.VerticalOffset}");
                return Task.CompletedTask;
            });

            // 如果第一次恢复不成功，再试一次
            if (Math.Abs(scrollViewer.VerticalOffset - position) > 1)
            {
                await Task.Delay(100);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    scrollViewer.ScrollToVerticalOffset(position);
                    Debug.WriteLine($"第二次尝试恢复滚动位置到：{position}，当前位置：{scrollViewer.VerticalOffset}");
                    return Task.CompletedTask;
                });
            }
        }

        private void PostsListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_pendingScrollOffset >= 0)
            {
                var scrollViewer = GetScrollViewer(PostsListView);
                if (scrollViewer != null)
                {
                    Debug.WriteLine($"恢复滚动位置到：{_pendingScrollOffset}");
                    scrollViewer.ScrollToVerticalOffset(_pendingScrollOffset);
                    _pendingScrollOffset = -1;
                }
            }
        }

        private void OpenPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PostViewModel post)
            {
                var postData = new Post
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    Url = post.Url,
                    Author = post.Author,
                    PostTime = post.PostTime,
                    LastReplyTime = post.LastReplyTime,
                    LastReplyUser = post.LastReplyUser,
                    ReplyCount = post.ReplyCount,
                    ViewCount = post.ViewCount
                };
                
                PostPreview.ShowPreview(postData, this);
            }
        }
    }
}