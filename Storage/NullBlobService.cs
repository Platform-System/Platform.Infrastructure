using Platform.Application.Abstractions.Storage;

namespace Platform.Infrastructure.Storage;

public sealed class NullBlobService : IBlobService
{
    public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string containerName)
    {
        throw new InvalidOperationException("Blob storage is not configured.");
    }

    public Task<(string BlobName, string ContainerName)> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Blob storage is not configured.");
    }

    public string GenerateReadSasUrl(string container, string blobName, int expireMinutes = 5)
    {
        return string.Empty;
    }

    public List<string> GenerateReadSasUrlsAsync(string container, IEnumerable<string> blobNames, int expireMinutes = 5)
    {
        return blobNames.Select(_ => string.Empty).ToList();
    }

    public Task<string> MakePublicAndGetUrl(string container, string blobName, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Blob storage is not configured.");
    }
}
