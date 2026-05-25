namespace MiniInstagram.Models;

public class Post
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Caption { get; set; } = string.Empty;
    public string ImagePath { get; set; } = null!;
    public string MediaType { get; set; } = "image";
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Data.ApplicationUser User { get; set; } = null!;
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Like> Likes { get; set; } = [];
    public ICollection<SavedPost> SavedByUsers { get; set; } = [];
    public ICollection<PostVisibleUser> VisibleUsers { get; set; } = [];
}
