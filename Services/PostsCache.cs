using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using zuanke8.ViewModels;
using System.Diagnostics;

namespace zuanke8.Services
{
    public class PostsCache
    {
        private const int PageSize = 20;
        private readonly List<PostViewModel> _allPosts;
        private readonly Dictionary<string, PostViewModel> _postCache;
        private readonly object _lock = new object();

        public PostsCache()
        {
            _allPosts = new List<PostViewModel>();
            _postCache = new Dictionary<string, PostViewModel>();
        }

        public void UpdatePosts(IEnumerable<Post> posts)
        {
            try
            {
                if (posts == null) return;

                foreach (var post in posts)
                {
                    if (post == null || string.IsNullOrEmpty(post.PostId)) continue;

                    if (!_postCache.TryGetValue(post.PostId, out var viewModel))
                    {
                        viewModel = new PostViewModel(post);
                        _postCache[post.PostId] = viewModel;
                        _allPosts.Add(viewModel);
                        Debug.WriteLine($"Added new post: {post.Title}");
                    }
                    else
                    {
                        // 更新现有帖子的属性
                        viewModel.UpdateFrom(post);
                        Debug.WriteLine($"Updated existing post: {post.Title}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating posts: {ex.Message}");
            }
        }

        public ObservableCollection<PostViewModel> GetPagedPosts(int pageIndex, bool orderByLastReply, bool showFavoritesOnly)
        {
            try
            {
                var query = _allPosts.Where(p => !p.IsHidden);

                if (showFavoritesOnly)
                {
                    query = query.Where(p => p.IsFavorite);
                }

                query = orderByLastReply
                    ? query.OrderByDescending(p => p.LastReplyTime)
                    : query.OrderByDescending(p => p.PostTime);

                var pagedPosts = query.Skip(pageIndex * PageSize).Take(PageSize);
                Debug.WriteLine($"Getting page {pageIndex}, posts count: {pagedPosts.Count()}");

                return new ObservableCollection<PostViewModel>(pagedPosts);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting paged posts: {ex.Message}");
                return new ObservableCollection<PostViewModel>();
            }
        }

        public int GetTotalPages(bool showFavoritesOnly)
        {
            try
            {
                var count = showFavoritesOnly
                    ? _allPosts.Count(p => !p.IsHidden && p.IsFavorite)
                    : _allPosts.Count(p => !p.IsHidden);

                return (count + PageSize - 1) / PageSize;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting total pages: {ex.Message}");
                return 0;
            }
        }

        public void Clear()
        {
            _allPosts.Clear();
            _postCache.Clear();
        }

        public IEnumerable<PostViewModel> GetAllPosts()
        {
            lock (_lock)
            {
                return _allPosts.ToList();
            }
        }
    }
} 