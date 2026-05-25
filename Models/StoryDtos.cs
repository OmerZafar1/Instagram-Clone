namespace MiniInstagram.Models;

public sealed record StoryTrayItem(
    string UserId,
    string UserName,
    string DisplayName,
    string? AvatarPath,
    DateTime LatestStoryAt,
    int StoryCount,
    int UnviewedCount,
    bool IsCurrentUser);

public sealed record StoryViewItem(
    int Id,
    string UserId,
    string UserName,
    string DisplayName,
    string? AvatarPath,
    string MediaPath,
    string MediaType,
    string? Caption,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    int ViewCount,
    bool IsViewed);

public sealed record StoryViewerItem(
    string UserId,
    string UserName,
    string DisplayName,
    string? AvatarPath,
    DateTime ViewedAt);
