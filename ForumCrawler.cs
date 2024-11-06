using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Text;
using System.Net;
using System.Diagnostics;

namespace zuanke8
{
    public class ForumCrawler
    {
        private const string ForumUrlTemplate = "http://www.zuanke8.com/forum-15-{0}.html";
        private const int MinPageCount = 10;  // 最少爬取页数
        private const int MinDelay = 800;     // 最小延迟（毫秒）
        private const int MaxDelay = 1200;    // 最大延迟（毫秒）
        
        private readonly HttpClient _client;
        private readonly Timer _timer;
        private readonly Action<ObservableCollection<Post>> _updateCallback;
        private readonly Random _random;
        private readonly PostManager _postManager;

        static ForumCrawler()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public ForumCrawler(Action<ObservableCollection<Post>> updateCallback, PostManager postManager)
        {
            _updateCallback = updateCallback;
            _postManager = postManager;
            _random = new Random();
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            _client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,zh-TW;q=0.8,en;q=0.7");
            _client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            _client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

            _timer = new Timer(async _ => await FetchPosts(), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start(int intervalSeconds)
        {
            var cookie = CookieManager.LoadCookie();
            if (string.IsNullOrEmpty(cookie))
            {
                MessageBox.Show("请先登录！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _client.DefaultRequestHeaders.Remove("Cookie");
            _client.DefaultRequestHeaders.Add("Cookie", cookie);

            _ = FetchPosts();

            _timer.Change(intervalSeconds * 1000, intervalSeconds * 1000);
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public async Task FetchPosts()
        {
            try
            {
                var allPosts = new ObservableCollection<Post>();
                bool shouldContinue = true;

                // 设置刷新状态为 true
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.IsRefreshing = true;
                    }
                });

                Debug.WriteLine($"开始爬取，时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // 爬取多页，直到遇到完全重复的页面或达到最大页数
                for (int page = 1; page <= MinPageCount && shouldContinue; page++)
                {
                    Debug.WriteLine($"正在爬取第 {page} 页...");
                    
                    var url = string.Format(ForumUrlTemplate, page);
                    var response = await _client.GetByteArrayAsync(url);
                    var html = Encoding.GetEncoding("GBK").GetString(response);
                    var pagePosts = ParsePosts(html);
                    
                    // 检查这一页的帖子是否都已存在
                    bool allExist = true;
                    foreach (var post in pagePosts)
                    {
                        allPosts.Add(post);
                        
                        // 检查是否为新帖子
                        var existingPost = _postManager.GetPost(post.PostId);
                        if (existingPost == null)
                        {
                            allExist = false;
                        }
                    }

                    // 如果这一页的所有帖子都已存在，停止爬取
                    if (allExist)
                    {
                        Debug.WriteLine($"第 {page} 页所有帖子都已存在，停止爬取");
                        shouldContinue = false;
                    }
                    else if (page < MinPageCount)
                    {
                        var delay = _random.Next(MinDelay, MaxDelay + 1);
                        Debug.WriteLine($"第 {page} 页爬取完成，获取到 {pagePosts.Count} 个帖子，等待 {delay}ms 后继续");
                        await Task.Delay(delay);
                    }
                    else
                    {
                        Debug.WriteLine($"第 {page} 页爬取完成，获取到 {pagePosts.Count} 个帖子");
                    }
                }

                Debug.WriteLine($"爬取结束，共获取到 {allPosts.Count} 个帖子");
                Application.Current.Dispatcher.Invoke(() => _updateCallback(allPosts));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"爬取失败：{ex.Message}");
                Application.Current.Dispatcher.Invoke(() => 
                    MessageBox.Show($"获取帖子失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                // 设置刷新状态为 false
                await Task.Delay(500); // 至少显示加载动画0.5秒
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.IsRefreshing = false;
                    }
                });
            }
        }

        private ObservableCollection<Post> ParsePosts(string html)
        {
            var posts = new ObservableCollection<Post>();
            
            // 使用正则表达式提取帖子信息
            var pattern = @"<tbody id=""normalthread_(\d+)"".*?class=""s xst""[^>]*>(.*?)</a>.*?<cite>\s*<a[^>]*>([^<]+)</a></cite>\s*<em><span[^>]*>(.*?)</span></em>.*?class=""xi2"">(\d+)</a><em>(\d+)</em>.*?<td class=""by"">.*?<cite><a[^>]*>([^<]+)</a></cite>\s*<em><a[^>]*>(.*?)</a></em>";
            
            var matches = Regex.Matches(html, pattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                try
                {
                    var post = new Post
                    {
                        PostId = match.Groups[1].Value,
                        Title = WebUtility.HtmlDecode(match.Groups[2].Value.Trim()),
                        Author = WebUtility.HtmlDecode(match.Groups[3].Value.Trim()),
                        PostTime = ParseTime(match.Groups[4].Value.Trim()),
                        ReplyCount = int.Parse(match.Groups[5].Value),
                        ViewCount = int.Parse(match.Groups[6].Value),
                        LastReplyUser = WebUtility.HtmlDecode(match.Groups[7].Value.Trim()),
                        LastReplyTime = ParseTime(match.Groups[8].Value.Trim()),
                        Url = $"http://www.zuanke8.com/thread-{match.Groups[1].Value}-1-1.html"
                    };
                    
                    posts.Add(post);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Parse post error: {ex.Message}");
                    Debug.WriteLine($"Post content: {match.Value}");
                }
            }

            return posts;
        }

        private DateTime ParseTime(string timeStr)
        {
            try
            {
                if (timeStr.Contains("天前"))
                {
                    int days = int.Parse(timeStr.Replace("天前", ""));
                    return DateTime.Now.AddDays(-days);
                }
                else if (timeStr.Contains("小时前"))
                {
                    int hours = int.Parse(timeStr.Replace("小时前", ""));
                    return DateTime.Now.AddHours(-hours);
                }
                else if (timeStr.Contains("分钟前"))
                {
                    int minutes = int.Parse(timeStr.Replace("分钟前", ""));
                    return DateTime.Now.AddMinutes(-minutes);
                }
                else if (timeStr.Contains("秒前"))
                {
                    int seconds = int.Parse(timeStr.Replace("秒前", ""));
                    return DateTime.Now.AddSeconds(-seconds);
                }
                else if (timeStr == "昨天")
                {
                    return DateTime.Now.AddDays(-1);
                }
                else
                {
                    // 处理具体时间格式，如：2024-11-6 22:53
                    return DateTime.Parse(timeStr);
                }
            }
            catch
            {
                return DateTime.Now;
            }
        }

        public async Task<ObservableCollection<Post>> FetchSinglePage(string url)
        {
            try
            {
                var response = await _client.GetByteArrayAsync(url);
                var html = Encoding.GetEncoding("GBK").GetString(response);
                return ParsePosts(html);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fetch single page error: {ex.Message}");
                return null;
            }
        }
    }
} 