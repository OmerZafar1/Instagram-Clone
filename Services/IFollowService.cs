using MiniInstagram.Data;
using MiniInstagram.Models;

namespace MiniInstagram.Services;

public interface IFollowService
{
    Task<FollowActionResult> ToggleFollowAsync(string followerId, string followingId, CancellationToken cancellationToken = default);
    Task<bool> IsFollowingAsync(string followerId, string followingId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingFollowRequestAsync(string requesterId, string targetUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FollowRequestDto>> GetPendingFollowRequestsAsync(string targetUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserListItemDto>> GetFollowersAsync(string userId, CancellationToken cancellationToken = default);
    Task AcceptFollowRequestAsync(int requestId, string targetUserId, CancellationToken cancellationToken = default);
    Task RejectFollowRequestAsync(int requestId, string targetUserId, CancellationToken cancellationToken = default);
    Task<int> GetFollowerCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> GetFollowingCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetProfileByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationUser>> SearchUsersAsync(string query, string? excludeUserId, int take = 20, CancellationToken cancellationToken = default);
}
