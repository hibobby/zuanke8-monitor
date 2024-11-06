using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;

namespace zuanke8
{
    public class CookieManager
    {
        private static readonly string CookieFile = "cookies.dat";
        private static readonly string Key = "YourSecretKey123"; // 加密密钥

        static CookieManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static void SaveCookie(string cookie)
        {
            try
            {
                string encryptedCookie = Encrypt(cookie);
                File.WriteAllText(CookieFile, encryptedCookie);
            }
            catch (Exception ex)
            {
                throw new Exception("保存Cookie失败", ex);
            }
        }

        public static string? LoadCookie()
        {
            try
            {
                if (!File.Exists(CookieFile)) return null;
                string encryptedCookie = File.ReadAllText(CookieFile);
                return Decrypt(encryptedCookie);
            }
            catch
            {
                return null;
            }
        }

        public static void ClearCookie()
        {
            try
            {
                if (File.Exists(CookieFile))
                    File.Delete(CookieFile);
            }
            catch { }
        }

        private static string Encrypt(string text)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key.PadRight(32, '0'));
                aes.IV = new byte[16];

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(text);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private static string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key.PadRight(32, '0'));
                aes.IV = new byte[16];

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        public static bool CheckLoginStatus()
        {
            var cookie = LoadCookie();
            if (string.IsNullOrEmpty(cookie)) return false;

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Cookie", cookie);
                    var response = client.GetByteArrayAsync("http://www.zuanke8.com/home.php").Result;
                    var content = Encoding.GetEncoding("GBK").GetString(response);
                    return !content.Contains("member.php?mod=logging&action=login");
                }
            }
            catch
            {
                return false;
            }
        }
    }
} 