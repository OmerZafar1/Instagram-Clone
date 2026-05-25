namespace MiniInstagram.Models;

public record FollowRequestDto(
    int Id,
    string RequesterId,
    string UserName,
    string DisplayName,
    string? AvatarPath,
    DateTime CreatedAt);
