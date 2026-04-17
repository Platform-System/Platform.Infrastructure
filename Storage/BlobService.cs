using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Platform.Application.Abstractions.Storage;
using Platform.BuildingBlocks.DateTimes;

namespace Platform.Infrastructure.Storage;

public sealed class BlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string containerName)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("Empty file");

        var container = _blobServiceClient.GetBlobContainerClient(containerName);

        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blob = container.GetBlobClient(Guid.NewGuid() + Path.GetExtension(fileName));

        if (fileStream.CanSeek)
            fileStream.Position = 0;

        await blob.UploadAsync(fileStream, new BlobHttpHeaders
        {
            ContentType = contentType
        });

        return blob.Uri.ToString();
    }

    public async Task<(string BlobName, string ContainerName)> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerName = "products-private";
        var blobName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);

        if (stream.CanSeek)
            stream.Position = 0;

        await blobClient.UploadAsync(stream, new BlobHttpHeaders
        {
            ContentType = contentType
        },
        cancellationToken: cancellationToken);

        return (blobName, containerName);
    }

    public string GenerateReadSasUrl(string container, string blobName, int expireMinutes = 5)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);

        var sasUri = blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            new DateTimeOffset(Clock.Now.AddMinutes(expireMinutes)));

        return sasUri.ToString();
    }

    public List<string> GenerateReadSasUrlsAsync(string container, IEnumerable<string> blobNames, int expireMinutes = 5)
    {
        return blobNames.Select(name => GenerateReadSasUrl(container, name, expireMinutes)).ToList();
    }

    public async Task<string> MakePublicAndGetUrl(string container, string blobName)
    {
        var sourceContainer = _blobServiceClient.GetBlobContainerClient(container);
        var sourceBlob = sourceContainer.GetBlobClient(blobName);

        if (!await sourceBlob.ExistsAsync())
            throw new FileNotFoundException("Blob not found");

        var publicContainerName = "products-public";
        var publicContainer = _blobServiceClient.GetBlobContainerClient(publicContainerName);
        publicContainer.CreateIfNotExists(PublicAccessType.Blob);

        var destBlob = publicContainer.GetBlobClient(blobName);

        await destBlob.StartCopyFromUriAsync(sourceBlob.Uri);

        BlobProperties properties;
        do
        {
            await Task.Delay(200);
            properties = await destBlob.GetPropertiesAsync();
        }
        while (properties.CopyStatus == CopyStatus.Pending);

        if (properties.CopyStatus != CopyStatus.Success)
            throw new Exception("Blob copy failed");

        await sourceBlob.DeleteIfExistsAsync();

        return destBlob.Uri.ToString();
    }
}
