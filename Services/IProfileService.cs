namespace MiniInstagram.Services;

public interface IProfileService
{
    Task UpdateProfileAsync(string userId, string displayName, string? bio, bool isPrivate, CancellationToken cancellationToken = default);
    Task<string?> UpdateAvatarAsync(string userId, Stream stream, string fileName, CancellationToken cancellationToken = default);
}
