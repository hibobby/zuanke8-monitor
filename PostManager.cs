using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace zuanke8
{
    public class PostManager
    {
        private List<Post> _posts;
        private readonly object _lock = new object();
        private static readonly string PostsFile = "posts.json";
        private const int MaxPostsCount = 1000; // 最大保存帖子数量

        public PostManager()
        {
            _posts = LoadPosts();
        }

        private List<Post> LoadPosts()
        {
            try
            {
                if (File.Exists(PostsFile))
                {
                    var json = File.ReadAllText(PostsFile);
                    var posts = JsonSerializer.Deserialize<List<Post>>(json) ?? new List<Post>();
                    CleanOldPosts(posts);
                    Debug.WriteLine($"加载了 {posts.Count} 个帖子");
                    return posts;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load posts error: {ex.Message}");
            }
            return new List<Post>();
        }

        private void SavePosts()
        {
            try
            {
                var json = JsonSerializer.Serialize(_posts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(PostsFile, json);
                Debug.WriteLine($"保存了 {_posts.Count} 个帖子到文件");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save posts error: {ex.Message}");
            }
        }

        public void AddPosts(IEnumerable<Post> newPosts)
        {
            lock (_lock)
            {
                bool hasChanges = false;
                foreach (var post in newPosts)
                {
                    Debug.WriteLine($"检查帖子 {post.PostId}: {post.Title}");
                    var existingPost = _posts.FirstOrDefault(p => p.PostId == post.PostId);
                    if (existingPost == null)
                    {
                        Debug.WriteLine($"新帖子：{post.Title}");
                        post.IsRead = false;
                        post.IsHidden = false;
                        post.IsFavorite = false;
                        _posts.Add(post);
                        hasChanges = true;
                    }
                    else
                    {
                        Debug.WriteLine($"更新帖子：{post.Title}");
                        existingPost.LastReplyTime = post.LastReplyTime;
                        existingPost.LastReplyUser = post.LastReplyUser;
                        existingPost.ReplyCount = post.ReplyCount;
                        existingPost.ViewCount = post.ViewCount;
                        existingPost.Title = post.Title;
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    CleanOldPosts(_posts);
                    SavePosts();
                    Debug.WriteLine($"保存后总帖子数：{_posts.Count}");
                }
            }
        }

        private void CleanOldPosts(List<Post> posts)
        {
            // 先按时间清理
            var threeDaysAgo = DateTime.Now.AddDays(-7);
            posts.RemoveAll(p => p.PostTime < threeDaysAgo);

            // 如果数量超过限制，按时间排序后只保留最新的
            if (posts.Count > MaxPostsCount)
            {
                posts = posts.OrderByDescending(p => p.PostTime)
                            .Take(MaxPostsCount)
                            .ToList();
                
                Debug.WriteLine($"清理后保留 {posts.Count} 个帖子");
            }
        }

        public ObservableCollection<Post> GetPosts(bool orderByLastReply = true)
        {
            lock (_lock)
            {
                var query = _posts.Where(p => !p.IsHidden);

                var orderedPosts = orderByLastReply
                    ? query.OrderByDescending(p => p.LastReplyTime)
                    : query.OrderByDescending(p => p.PostTime);

                return new ObservableCollection<Post>(orderedPosts);
            }
        }

        public void UpdatePost(Post post)
        {
            lock (_lock)
            {
                var existingPost = _posts.FirstOrDefault(p => p.PostId == post.PostId);
                if (existingPost != null)
                {
                    existingPost.IsRead = post.IsRead;
                    existingPost.IsHidden = post.IsHidden;
                    existingPost.IsFavorite = post.IsFavorite;
                    existingPost.IsHighlight = post.IsHighlight;
                    existingPost.LastReplyTime = post.LastReplyTime;
                    existingPost.LastReplyUser = post.LastReplyUser;
                    existingPost.ReplyCount = post.ReplyCount;
                    existingPost.ViewCount = post.ViewCount;
                    SavePosts();
                }
                else
                {
                    _posts.Add(post);
                    SavePosts();
                }
            }
        }

        public Post GetPost(string postId)
        {
            lock (_lock)
            {
                var post = _posts.FirstOrDefault(p => p.PostId == postId);
                if(post == null)
                {
                    Debug.WriteLine($"查找帖子 {postId}: 不存在");
                }
                return post;
            }
        }
    }
} 