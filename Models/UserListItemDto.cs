namespace MiniInstagram.Models;

public record UserListItemDto(
    string UserId,
    string UserName,
    string DisplayName,
    string? AvatarPath);
