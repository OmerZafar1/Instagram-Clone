namespace MiniInstagram.Services;

public class LocalImageStorageService(IWebHostEnvironment environment) : IImageStorageService
{
    private const string UploadRoot = "uploads";

    public async Task<string> SavePostImageAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        return await SaveAsync(stream, Path.Combine(UploadRoot, "posts"), fileName, cancellationToken);
    }

    public async Task<string> SavePostMediaAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        return await SaveAsync(stream, Path.Combine(UploadRoot, "posts"), fileName, cancellationToken);
    }

    public async Task<string> SaveAvatarAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        return await SaveAsync(stream, Path.Combine(UploadRoot, "avatars"), fileName, cancellationToken);
    }

    public async Task<string> SaveVoiceAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        return await SaveAsync(stream, Path.Combine(UploadRoot, "voice"), fileName, cancellationToken);
    }

    public async Task<string> SaveStoryMediaAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        return await SaveAsync(stream, Path.Combine(UploadRoot, "stories"), fileName, cancellationToken);
    }

    public bool DeleteIfExists(string relativePath)
    {
        var fullPath = Path.Combine(environment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            return false;
        }

        File.Delete(fullPath);
        return true;
    }

    private async Task<string> SaveAsync(Stream stream, string subFolder, string fileName, CancellationToken cancellationToken)
    {
        var safeName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var relativePath = Path.Combine(subFolder, safeName).Replace('\\', '/');
        var fullPath = Path.Combine(environment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return relativePath;
    }
}
