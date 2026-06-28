using MedFund.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedFund.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly StorageOptions options;
    private readonly ILogger<LocalFileStorageService> logger;

    public LocalFileStorageService(IOptions<StorageOptions> options, ILogger<LocalFileStorageService> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<StoredFile> SaveAsync(string folder, FileUploadDescriptor file, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received file save request. Folder={Folder}, FileName={FileName}, ContentType={ContentType}, SizeBytes={SizeBytes}", nameof(LocalFileStorageService), nameof(SaveAsync), folder, file.FileName, file.ContentType, file.SizeBytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        var safeFileName = Path.GetFileName(file.FileName);
        var relativeFolder = folder.Replace('\\', '/').Trim('/');
        var storageKey = $"{relativeFolder}/{Guid.NewGuid():N}-{safeFileName}";
        var root = Path.IsPathRooted(options.LocalRoot)
            ? options.LocalRoot
            : Path.Combine(AppContext.BaseDirectory, options.LocalRoot);
        var fullPath = Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath) ?? root;

        Directory.CreateDirectory(directory);
        await using var output = File.Create(fullPath);
        await file.Content.CopyToAsync(output, cancellationToken);
        logger.LogInformation("{Service}.{Function} saved local file. StorageKey={StorageKey}, SizeBytes={SizeBytes}", nameof(LocalFileStorageService), nameof(SaveAsync), storageKey, file.SizeBytes);

        return new StoredFile(storageKey, safeFileName, file.ContentType, file.SizeBytes);
    }
}
