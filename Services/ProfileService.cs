using Microsoft.EntityFrameworkCore;
using MiniInstagram.Data;

namespace MiniInstagram.Services;

public class ProfileService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IImageStorageService imageStorage) : IProfileService
{
    public async Task UpdateProfileAsync(string userId, string displayName, string? bio, bool isPrivate, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        user.DisplayName = displayName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        user.IsPrivate = isPrivate;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> UpdateAvatarAsync(
        string userId,
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstAsync(u => u.Id == userId, cancellationToken);

        if (!string.IsNullOrEmpty(user.AvatarPath))
        {
            imageStorage.DeleteIfExists(user.AvatarPath);
        }

        user.AvatarPath = await imageStorage.SaveAvatarAsync(stream, fileName, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return user.AvatarPath;
    }
}
