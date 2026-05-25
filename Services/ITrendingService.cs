using MiniInstagram.Models;

namespace MiniInstagram.Services;

public interface ITrendingService
{
    Task<IReadOnlyList<TrendingPostDto>> GetTrendingPostsAsync(int hours = 24, int take = 12, CancellationToken ct = default);
}
