using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace zuanke8
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AppSettings _settings;
        private ObservableCollection<Post> _allPosts;
        public ObservableCollection<Post> Posts { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Instance;
            Posts = new ObservableCollection<Post>();
            InitializeTestData();
            ApplySettings();

            // 监听设置变化
            _settings.Blacklist.CollectionChanged += (s, e) => ApplySettings();
            _settings.Highlights.CollectionChanged += (s, e) => ApplySettings();
        }

        private void InitializeTestData()
        {
            _allPosts = new ObservableCollection<Post>
            {
                new Post
                {
                    Title = "爱奇艺越来越过了",
                    Author = "v1y20086688",
                    PostTime = DateTime.Parse("2024-11-6 21:01"),
                    ReplyCount = 0,
                    ViewCount = 69,
                    LastReplyUser = "v1y20086688",
                    LastReplyTime = DateTime.Parse("2024-11-6 21:01")
                },
                new Post
                {
                    Title = "我研究了下，PLUS有洗车洗衣服服的，如果洗车无刷洗，优先选洗衣比较划算",
                    Author = "夜半窗外转",
                    PostTime = DateTime.Parse("2024-11-3 00:58"),
                    ReplyCount = 25,
                    ViewCount = 1319,
                    LastReplyUser = "古茗231",
                    LastReplyTime = DateTime.Parse("2024-11-6 21:00")
                },
                new Post
                {
                    Title = "服了，京东家政天天出去嗨",
                    Author = "wilwri",
                    PostTime = DateTime.Parse("2024-11-6 20:51"),
                    ReplyCount = 4,
                    ViewCount = 299,
                    LastReplyUser = "我来看看的",
                    LastReplyTime = DateTime.Parse("2024-11-6 20:59")
                },
                new Post
                {
                    Title = "【每日必看】双十一各大平台隐藏优惠券合集",
                    Author = "优惠券达人",
                    PostTime = DateTime.Parse("2024-11-6 20:45"),
                    ReplyCount = 156,
                    ViewCount = 2891,
                    LastReplyUser = "省钱小能手",
                    LastReplyTime = DateTime.Parse("2024-11-6 20:58")
                },
                new Post
                {
                    Title = "建议收藏！2024年各大电商平台优惠日历",
                    Author = "折扣研究员",
                    PostTime = DateTime.Parse("2024-11-6 20:30"),
                    ReplyCount = 89,
                    ViewCount = 1567,
                    LastReplyUser = "购物达人",
                    LastReplyTime = DateTime.Parse("2024-11-6 20:57")
                },
                new Post
                {
                    Title = "白条还款日期选择攻略，让你的信用卡更省心",
                    Author = "理财专家",
                    PostTime = DateTime.Parse("2024-11-6 20:20"),
                    ReplyCount = 45,
                    ViewCount = 876,
                    LastReplyUser = "金融小白",
                    LastReplyTime = DateTime.Parse("2024-11-6 20:56")
                },
                new Post
                {
                    Title = "实测：各大平台相似商品价格对比，别被忽悠了",
                    Author = "明察秋毫",
                    PostTime = DateTime.Parse("2024-11-6 20:10"),
                    ReplyCount = 67,
                    ViewCount = 1234,
                    LastReplyUser = "理性消费",
                    LastReplyTime = DateTime.Parse("2024-11-6 20:55")
                },
                new Post
                {
                    Title = "【整理】京东PLUS会员权益最新变化，值得续费吗",
                    Author = "会员研究所",
                    PostTime = DateTime.Parse("2024-11-6 20:00"),
                    ReplyCount = 234,
                    ViewCount = 3456,
                    LastReplyUser = "权益达人",
                    LastReplyTime = DateTime.Parse("2024-11-6 20:54")
                },
                new Post
                {
                    Title = "618购物节提前预热，各平台活动规则解析",
                    Author = "购物指南",
                    PostTime = DateTime.Parse("2024-11-6 19:50"),
                    ReplyCount = 178,
                    ViewCount = 2567,
                    LastReplyUser = "剁手党",
                    LastReplyTime = DateTime.Parse("2024-11-6 20:53")
                },
                new Post
                {
                    Title = "信用卡还款技巧：如何合理利用免息期",
                    Author = "金融顾问",
                    PostTime = DateTime.Parse("2024-11-6 19:40"),
                    ReplyCount = 145,
                    ViewCount = 1987,
                    LastReplyUser = "理财新手",
                    LastReplyTime = DateTime.Parse("2024-11-6 20:52")
                },
                new Post
                {
                    Title = "【经验分享】如何避免网购中的常见陷阱",
                    Author = "网购老手",
                    PostTime = DateTime.Parse("2024-11-6 19:30"),
                    ReplyCount = 198,
                    ViewCount = 2765,
                    LastReplyUser = "精明眼",
                    LastReplyTime = DateTime.Parse("2024-11-6 20:51")
                },
                new Post
                {
                    Title = "各大平台优惠券叠加规则详解",
                    Author = "券妈妈",
                    PostTime = DateTime.Parse("2024-11-6 19:20"),
                    ReplyCount = 167,
                    ViewCount = 2345,
                    LastReplyUser = "省钱达人",
                    LastReplyTime = DateTime.Parse("2024-11-6 20:50")
                }
            };
            
            ApplySettings();
        }

        private void ApplySettings()
        {
            // 清空当前显示的帖子
            Posts.Clear();

            foreach (var post in _allPosts)
            {
                // 检查是否在黑名单中
                bool isBlacklisted = _settings.Blacklist.Any(keyword => post.ContainsKeyword(keyword));
                if (isBlacklisted) continue;

                // 检查是否需要高亮
                post.IsHighlight = _settings.Highlights.Any(keyword => post.ContainsKeyword(keyword));

                // 添加到显示列表
                Posts.Add(post);
            }

            // 更新ListView的数据源
            PostsListView.ItemsSource = null;
            PostsListView.ItemsSource = Posts;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现登录逻辑
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
    }
}