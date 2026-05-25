namespace MiniInstagram.Models;

public record FeedPageDto(
    IReadOnlyList<Post> Items,
    DateTime? NextBeforeCreatedAt,
    int? NextBeforeId,
    bool HasMore);
