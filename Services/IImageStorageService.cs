namespace MiniInstagram.Services;

public interface IImageStorageService
{
    Task<string> SavePostImageAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<string> SavePostMediaAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<string> SaveAvatarAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<string> SaveVoiceAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<string> SaveStoryMediaAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    bool DeleteIfExists(string relativePath);
}
