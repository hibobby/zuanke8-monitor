using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;

namespace zuanke8
{
    public partial class Login : Window
    {
        private const string LoginUrl = "http://www.zuanke8.com/member.php?mod=logging&action=login";
        private const string SuccessUrl = "http://www.zuanke8.com/forum.php";

        public Login()
        {
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoginBrowser.EnsureCoreWebView2Async();
            LoginBrowser.CoreWebView2.Navigate(LoginUrl);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void LoginBrowser_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;

            var currentUrl = LoginBrowser.Source.ToString();

            // 检查是否已经登录成功（重定向到首页或其他页面）
            if (!currentUrl.Contains("action=login"))
            {
                try
                {
                    // 获取页面内容检查登录状态
                    var content = await LoginBrowser.CoreWebView2.ExecuteScriptAsync(
                        "document.documentElement.outerHTML");

                    if (!content.Contains("member.php?mod=logging&amp;action=login"))
                    {
                        // 获取所有Cookie
                        var allCookies = await LoginBrowser.CoreWebView2.CookieManager.GetCookiesAsync("http://www.zuanke8.com");
                        var cookieList = new List<string>();

                        foreach (var cookie in allCookies)
                        {
                            // 只保存需要的Cookie
                            if (cookie.Name.StartsWith("ki1e_2132_"))
                            {
                                cookieList.Add($"{cookie.Name}={cookie.Value}");
                            }
                        }

                        var cookieString = string.Join("; ", cookieList);

                        // 保存Cookie
                        if (!string.IsNullOrEmpty(cookieString))
                        {
                            CookieManager.SaveCookie(cookieString);
                            MessageBox.Show("登录成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // 通知主窗口登录成功
                            if (Owner is MainWindow mainWindow)
                            {
                                mainWindow.OnLoginSuccess();
                            }
                            
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("未能获取到登录Cookie", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存Cookie失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
} 