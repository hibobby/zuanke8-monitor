using System;

namespace zuanke8
{
    public class Post
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public DateTime PostTime { get; set; }
        public int ReplyCount { get; set; }
        public string Url { get; set; }
        public string PostId { get; set; }
        public string LastReplyUser { get; set; }
        public DateTime LastReplyTime { get; set; }
        public int ViewCount { get; set; }
        public bool HasAttachment { get; set; }
        public bool IsHighlight { get; set; }

        public bool ContainsKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
            return Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                   Author.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }
    }
} 