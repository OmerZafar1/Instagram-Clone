namespace MiniInstagram.Models;

public record TrendingPostDto(
    int PostId,
    string UserName,
    string DisplayName,
    string? AvatarPath,
    string ImagePath,
    string MediaType,
    string? Caption,
    int LikeCount,
    int CommentCount,
    int Score,
    DateTime CreatedAt);
