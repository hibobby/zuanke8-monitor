using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace zuanke8.ViewModels
{
    public class PostViewModel : INotifyPropertyChanged
    {
        private readonly Post _post;
        private bool _isRead;
        private bool _isHidden;
        private bool _isFavorite;
        private bool _isHighlight;
        private string _title;
        private string _author;
        private DateTime _postTime;
        private int _replyCount;
        private string _url;
        private string _postId;
        private string _lastReplyUser;
        private DateTime _lastReplyTime;
        private int _viewCount;

        public PostViewModel(Post post)
        {
            _post = post;
            _isRead = post.IsRead;
            _isHidden = post.IsHidden;
            _isFavorite = post.IsFavorite;
            _isHighlight = post.IsHighlight;
            _title = post.Title;
            _author = post.Author;
            _postTime = post.PostTime;
            _replyCount = post.ReplyCount;
            _url = post.Url;
            _postId = post.PostId;
            _lastReplyUser = post.LastReplyUser;
            _lastReplyTime = post.LastReplyTime;
            _viewCount = post.ViewCount;
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    _post.Title = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Author
        {
            get => _author;
            set
            {
                if (_author != value)
                {
                    _author = value;
                    _post.Author = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime PostTime
        {
            get => _postTime;
            set
            {
                if (_postTime != value)
                {
                    _postTime = value;
                    _post.PostTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ReplyCount
        {
            get => _replyCount;
            set
            {
                if (_replyCount != value)
                {
                    _replyCount = value;
                    _post.ReplyCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Url => _url;
        public string PostId => _postId;
        public string LastReplyUser
        {
            get => _lastReplyUser;
            set
            {
                if (_lastReplyUser != value)
                {
                    _lastReplyUser = value;
                    _post.LastReplyUser = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastReplyTime
        {
            get => _lastReplyTime;
            set
            {
                if (_lastReplyTime != value)
                {
                    _lastReplyTime = value;
                    _post.LastReplyTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ViewCount
        {
            get => _viewCount;
            set
            {
                if (_viewCount != value)
                {
                    _viewCount = value;
                    _post.ViewCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRead
        {
            get => _isRead;
            set
            {
                if (_isRead != value)
                {
                    _isRead = value;
                    _post.IsRead = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                if (_isHidden != value)
                {
                    _isHidden = value;
                    _post.IsHidden = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    _post.IsFavorite = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsHighlight
        {
            get => _isHighlight;
            set
            {
                if (_isHighlight != value)
                {
                    _isHighlight = value;
                    _post.IsHighlight = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ContainsKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
            return Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                   Author.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateFrom(Post post)
        {
            // 更新可能变化的属性
            if (post.LastReplyTime != LastReplyTime)
            {
                _post.LastReplyTime = post.LastReplyTime;
                OnPropertyChanged(nameof(LastReplyTime));
            }
            
            if (post.LastReplyUser != LastReplyUser)
            {
                _post.LastReplyUser = post.LastReplyUser;
                OnPropertyChanged(nameof(LastReplyUser));
            }
            
            if (post.ReplyCount != ReplyCount)
            {
                _post.ReplyCount = post.ReplyCount;
                OnPropertyChanged(nameof(ReplyCount));
            }
            
            if (post.ViewCount != ViewCount)
            {
                _post.ViewCount = post.ViewCount;
                OnPropertyChanged(nameof(ViewCount));
            }
        }
    }
} 