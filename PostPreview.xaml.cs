using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace zuanke8
{
    public partial class PostPreview : Window
    {
        private readonly Post _post;
        private static PostPreview? _currentPreview;
        private readonly ForumCrawler _crawler;

        public PostPreview(Post post)
        {
            InitializeComponent();
            DataContext = post;
            _post = post;
            _crawler = new ForumCrawler(null, null);
            
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                await WebView.EnsureCoreWebView2Async();
                
                // 配置 WebView2
                WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

                // 处理新窗口打开请求
                WebView.CoreWebView2.NewWindowRequested += (s, e) =>
                {
                    e.Handled = true;
                    Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
                };

                // 加载内容
                await LoadPostContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化预览窗口失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async Task LoadPostContent()
        {
            try
            {
                var content = await _crawler.FetchPostContent(_post.Url);
                
                // 创建完整的 HTML 文档
                var html = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
                        <base href=""https://www.zuanke8.com/"">
                        <style>
                            body {{
                                margin: 20px;
                                font-size: 15px;
                                line-height: 1.8;
                                background: #FFFFFF;
                                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
                                color: #1A1A1A;
                                font-weight: 500;
                                letter-spacing: 0.2px;
                            }}
                            img {{
                                max-width: 100%;
                                height: auto;
                                cursor: pointer;
                                display: block;
                                margin: 15px 0;
                                border-radius: 12px;
                                box-shadow: 0 4px 12px rgba(124, 77, 255, 0.1);
                                transition: transform 0.2s;
                            }}
                            img:hover {{
                                transform: scale(1.01);
                            }}
                            a {{
                                color: #7C4DFF;
                                text-decoration: none;
                                font-weight: 600;
                                transition: all 0.2s;
                                border-bottom: 1px solid transparent;
                            }}
                            a:hover {{
                                color: #6039CC;
                                border-bottom: 1px solid #6039CC;
                            }}
                            p {{
                                margin: 1.2em 0;
                                text-align: justify;
                                line-height: 1.8;
                            }}
                            ::-webkit-scrollbar {{
                                width: 8px;
                                height: 8px;
                            }}
                            ::-webkit-scrollbar-track {{
                                background: #F5F5F5;
                                border-radius: 4px;
                            }}
                            ::-webkit-scrollbar-thumb {{
                                background: #D8D8D8;
                                border-radius: 4px;
                                transition: background 0.2s;
                            }}
                            ::-webkit-scrollbar-thumb:hover {{
                                background: #7C4DFF;
                            }}
                            .t_f {{
                                white-space: pre-wrap;
                                word-wrap: break-word;
                                padding: 20px;
                                background: #FAFAFA;
                                border-radius: 12px;
                                border: 1px solid #F0F0F0;
                                box-shadow: 0 2px 8px rgba(124, 77, 255, 0.05);
                                margin-bottom: 20px;
                            }}
                            .t_f > br + br {{
                                display: none;
                            }}
                            .t_f > br {{
                                margin-bottom: 0.8em;
                            }}
                            /* 代码块样式 */
                            pre, code {{
                                background: #2D2B55;
                                color: #FFFFFF;
                                padding: 12px 16px;
                                border-radius: 8px;
                                font-family: 'JetBrains Mono', Consolas, Monaco, 'Courier New', monospace;
                                font-size: 14px;
                                line-height: 1.6;
                                margin: 15px 0;
                            }}
                            /* 引用样式 */
                            blockquote {{
                                margin: 1.5em 0;
                                padding: 12px 20px;
                                border-left: 4px solid #7C4DFF;
                                background: #F8F7FF;
                                color: #4A4A4A;
                                border-radius: 0 8px 8px 0;
                                font-style: italic;
                            }}
                            /* 强调文本 */
                            strong, b {{
                                color: #7C4DFF;
                                font-weight: 600;
                            }}
                            /* 列表样式 */
                            ul, ol {{
                                padding-left: 24px;
                                margin: 1em 0;
                            }}
                            li {{
                                margin: 0.5em 0;
                            }}
                            /* 表格样式 */
                            table {{
                                border-collapse: collapse;
                                width: 100%;
                                margin: 1.5em 0;
                                border-radius: 8px;
                                overflow: hidden;
                            }}
                            th, td {{
                                padding: 12px 16px;
                                border: 1px solid #E8E8E8;
                            }}
                            th {{
                                background: #7C4DFF;
                                color: white;
                                font-weight: 600;
                            }}
                            tr:nth-child(even) {{
                                background: #F8F7FF;
                            }}
                        </style>
                    </head>
                    <body>
                        {content}
                        <script>
                            document.querySelectorAll('img').forEach(function(img) {{
                                img.onclick = function() {{
                                    window.open(this.src, '_blank');
                                }};
                            }});
                            
                            document.querySelectorAll('a').forEach(function(a) {{
                                a.target = '_blank';
                            }});
                            
                            document.querySelectorAll('.t_f').forEach(function(content) {{
                                if (!content.querySelector('img') && !content.querySelector('a')) {{
                                    content.innerHTML = content.innerHTML
                                        .replace(/(<br\\s*\\/?>\s*){{3,}}/gi, '<br><br>')
                                        .split(/\\n\\s*\\n/)
                                        .map(function(text) {{ 
                                            return '<p>' + text.trim() + '</p>'; 
                                        }})
                                        .join('');
                                }}
                            }});
                        </script>
                    </body>
                    </html>";

                // 直接设置 HTML 内容
                WebView.CoreWebView2.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载帖子内容失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void ShowPreview(Post post, Window owner)
        {
            try
            {
                // 如果有已打开的预览窗口，先关闭它
                if (_currentPreview != null)
                {
                    _currentPreview.Close();
                    _currentPreview = null;
                }

                var preview = new PostPreview(post);
                _currentPreview = preview;
                
                // 计算预览窗口的位置
                var ownerRect = new Rect(owner.Left, owner.Top, owner.Width, owner.Height);
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;

                // 默认尝试在右侧显示
                double left = ownerRect.Right + 10;
                
                // 如果右侧空间不足，则显示在左侧
                if (left + preview.Width > screenWidth)
                {
                    left = ownerRect.Left - preview.Width - 10;
                }

                // 确保窗口不会超出屏幕顶部和底部
                double top = ownerRect.Top;
                if (top + preview.Height > screenHeight)
                {
                    top = screenHeight - preview.Height;
                }
                if (top < 0) top = 0;

                preview.Left = left;
                preview.Top = top;
                preview.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开预览窗口失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 清除当前预览窗口引用
            if (_currentPreview == this)
            {
                _currentPreview = null;
            }

            // 清理资源
            WebView?.Dispose();
        }
    }
} 