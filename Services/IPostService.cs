using MiniInstagram.Models;

namespace MiniInstagram.Services;

public interface IPostService
{
    Task<Post> CreatePostAsync(string userId, string caption, Stream mediaStream, string fileName, string mediaType, PostVisibility visibility = PostVisibility.Public, IReadOnlyCollection<string>? selectedViewerIds = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetFeedAsync(string userId, int take = 20, CancellationToken cancellationToken = default);
    Task<FeedPageDto> GetFeedPageAsync(string userId, DateTime? beforeCreatedAt = null, int? beforeId = null, int take = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetUserPostsAsync(string userId, int take = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetUserPostsForViewerAsync(string userId, string? viewerId, int take = 50, CancellationToken cancellationToken = default);
    Task<int> GetUserPostCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetSavedPostsAsync(string userId, int take = 50, CancellationToken cancellationToken = default);
    Task<Post?> GetPostAsync(int postId, CancellationToken cancellationToken = default);
    Task<Post?> GetPostForViewerAsync(int postId, string? viewerId, CancellationToken cancellationToken = default);
    Task<bool> ToggleLikeAsync(int postId, string userId, CancellationToken cancellationToken = default);
    Task<bool> ToggleSaveAsync(int postId, string userId, CancellationToken cancellationToken = default);
    Task<Comment> AddCommentAsync(int postId, string userId, string content, CancellationToken cancellationToken = default);
    Task<Comment> UpdateCommentAsync(int commentId, string userId, string content, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(int commentId, string userId, CancellationToken cancellationToken = default);
}
