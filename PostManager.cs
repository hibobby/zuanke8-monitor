using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

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
                    return posts;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load posts error: {ex.Message}");
            }
            return new List<Post>();
        }

        private void SavePosts()
        {
            try
            {
                var json = JsonSerializer.Serialize(_posts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(PostsFile, json);
                System.Diagnostics.Debug.WriteLine($"Saved {_posts.Count} posts to file");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save posts error: {ex.Message}");
            }
        }

        public void AddPosts(IEnumerable<Post> newPosts)
        {
            lock (_lock)
            {
                bool hasChanges = false;
                foreach (var post in newPosts)
                {
                    var existingPost = _posts.FirstOrDefault(p => p.PostId == post.PostId);
                    if (existingPost == null)
                    {
                        post.IsRead = false;
                        post.IsHidden = false;
                        post.IsFavorite = false;
                        _posts.Add(post);
                        hasChanges = true;
                    }
                    else
                    {
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
                }
            }
        }

        private void CleanOldPosts(List<Post> posts)
        {
            // 先按时间清理
            //var threeDaysAgo = DateTime.Now.AddDays(-3);
            //posts.RemoveAll(p => p.PostTime < threeDaysAgo);

            // 如果数量超过限制，按时间排序后只保留最新的
            if (posts.Count > MaxPostsCount)
            {
                posts = posts.OrderByDescending(p => p.LastReplyTime)
                            .Take(MaxPostsCount)
                            .ToList();
                
                System.Diagnostics.Debug.WriteLine($"清理后保留 {posts.Count} 个帖子");
            }
        }

        public ObservableCollection<Post> GetPosts(bool orderByLastReply = true)
        {
            lock (_lock)
            {
                var orderedPosts = orderByLastReply
                    ? _posts.OrderByDescending(p => p.LastReplyTime)
                    : _posts.OrderByDescending(p => p.PostTime);

                return new ObservableCollection<Post>(orderedPosts);
            }
        }

        public ObservableCollection<Post> GetAllPosts()
        {
            return new ObservableCollection<Post>(_posts);
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
                return _posts.FirstOrDefault(p => p.PostId == postId);
            }
        }
    }
} 