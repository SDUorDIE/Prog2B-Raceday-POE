using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayAPI.Services;

public sealed class ImageStorageService(IConfiguration configuration)
{
    public const long MaximumImageBytes = 5 * 1024 * 1024;

    public async Task<string> UploadAsync(string containerName, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaximumImageBytes)
            throw new InvalidImageException("Choose an image smaller than 5 MB.");

        var contentType = file.ContentType.ToLowerInvariant();
        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new InvalidImageException("Use a JPG, PNG or WebP image.")
        };

        await using var input = file.OpenReadStream();
        var bytes = new byte[checked((int)file.Length)];
        await input.ReadExactlyAsync(bytes, cancellationToken);
        if (!HasValidImageSignature(bytes, contentType))
            throw new InvalidImageException("The selected file is not a valid JPG, PNG or WebP image.");

        var blobName = $"{Guid.NewGuid():N}{extension}";
        var container = GetServiceClient().GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobName);
        await using var content = new MemoryStream(bytes, writable: false);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, cancellationToken);

        return blobName;
    }

    public async Task<(byte[] Content, string ContentType)?> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var blob = GetServiceClient().GetBlobContainerClient(containerName).GetBlobClient(blobName);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var response = await blob.DownloadContentAsync(cancellationToken);
        var contentType = response.Value.Details.ContentType;
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp")) return null;
        return (response.Value.Content.ToArray(), contentType);
    }

    private BlobServiceClient GetServiceClient()
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        return new BlobServiceClient(connectionString);
    }

    private static bool HasValidImageSignature(byte[] bytes, string contentType) => contentType switch
    {
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        "image/png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/webp" => bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };
}

public sealed class InvalidImageException(string message) : Exception(message);
