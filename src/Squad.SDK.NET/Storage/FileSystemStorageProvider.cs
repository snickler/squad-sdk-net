using Microsoft.Extensions.Logging;

namespace Squad.SDK.NET.Storage;

/// <summary>
/// File-system-backed implementation of <see cref="IStorageProvider"/> that persists values as individual files.
/// </summary>
public sealed class FileSystemStorageProvider : IStorageProvider
{
    private readonly string _rootPath;
    private readonly ILogger<FileSystemStorageProvider> _logger;

    /// <summary>
    /// Initializes a new <see cref="FileSystemStorageProvider"/> rooted at the specified directory.
    /// </summary>
    /// <param name="rootPath">Root directory for stored files; created if it does not exist.</param>
    /// <param name="logger">Logger instance.</param>
    public FileSystemStorageProvider(string rootPath, ILogger<FileSystemStorageProvider> logger)
    {
        _rootPath = rootPath;
        _logger = logger;
        Directory.CreateDirectory(_rootPath);
    }

    private string GetFilePath(string key) => Path.Combine(_rootPath, key.Replace('/', Path.DirectorySeparatorChar));

    /// <inheritdoc />
    public async Task<string?> ReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetFilePath(key);
        if (!File.Exists(path)) return null;
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var path = GetFilePath(key);
        var dir = Path.GetDirectoryName(path);
        if (dir is not null) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(path, value, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(GetFilePath(key)));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetFilePath(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListAsync(string prefix = "", CancellationToken cancellationToken = default)
    {
        var searchPath = string.IsNullOrEmpty(prefix) ? _rootPath : Path.Combine(_rootPath, prefix.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(searchPath))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var files = Directory.GetFiles(searchPath, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(_rootPath, f).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(f => string.IsNullOrEmpty(prefix) || f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Order()
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(files.AsReadOnly());
    }

    /// <inheritdoc />
    public Task<StorageStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_rootPath))
            return Task.FromResult(new StorageStats());

        var files = Directory.GetFiles(_rootPath, "*", SearchOption.AllDirectories);
        long totalSize = 0;
        DateTimeOffset? lastModified = null;

        foreach (var file in files)
        {
            var info = new FileInfo(file);
            totalSize += info.Length;
            var modified = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);
            if (lastModified is null || modified > lastModified)
                lastModified = modified;
        }

        return Task.FromResult(new StorageStats
        {
            ItemCount = files.Length,
            TotalSizeBytes = totalSize,
            LastModified = lastModified
        });
    }
}
