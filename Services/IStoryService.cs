using MiniInstagram.Models;

namespace MiniInstagram.Services;

public interface IStoryService
{
    Task<Story> CreateStoryAsync(
        string userId,
        string? caption,
        Stream mediaStream,
        string fileName,
        string mediaType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoryTrayItem>> GetStoryTrayAsync(
        string currentUserId,
        int take = 25,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoryViewItem>> GetVisibleStoriesByUserAsync(
        string ownerId,
        string viewerId,
        CancellationToken cancellationToken = default);

    Task<bool> HasVisibleActiveStoryAsync(
        string ownerId,
        string viewerId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkViewedAsync(
        int storyId,
        string viewerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoryViewerItem>> GetStoryViewersAsync(
        int storyId,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<ChatMessageDto?> ReactToStoryAsync(
        int storyId,
        string viewerId,
        string reaction,
        CancellationToken cancellationToken = default);

    Task<ChatMessageDto?> ReplyToStoryAsync(
        int storyId,
        string viewerId,
        string reply,
        CancellationToken cancellationToken = default);

    Task<int> DeleteExpiredStoriesAsync(CancellationToken cancellationToken = default);
}
