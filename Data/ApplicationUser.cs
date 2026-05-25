using Microsoft.AspNetCore.Identity;
using MiniInstagram.Models;

namespace MiniInstagram.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarPath { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Post> Posts { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Like> Likes { get; set; } = [];
    public ICollection<SavedPost> SavedPosts { get; set; } = [];
    public ICollection<PostVisibleUser> VisiblePosts { get; set; } = [];
    public ICollection<UserFollow> Followers { get; set; } = [];
    public ICollection<UserFollow> Following { get; set; } = [];
    public ICollection<FollowRequest> FollowRequestsSent { get; set; } = [];
    public ICollection<FollowRequest> FollowRequestsReceived { get; set; } = [];
    public ICollection<Story> Stories { get; set; } = [];
    public ICollection<StoryView> StoryViews { get; set; } = [];
}
