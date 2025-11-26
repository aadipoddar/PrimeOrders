using Azure.Storage;
using Azure.Storage.Blobs;

namespace PrimeBakesLibrary.DataAccess;

public static class BlobStorageAccess
{
    public static async Task<string> UploadFileToBlobStorage(Stream file, string fileName, BlobStorageContainers container)
    {
        BlobContainerClient containerClient = new(Secrets.AzureBlobStorageConnectionString, container.ToString());

        await containerClient.CreateIfNotExistsAsync();
        await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);

        BlobClient blobClient = containerClient.GetBlobClient(fileName);

        await blobClient.UploadAsync(file, true);

        return blobClient.Uri.ToString();
    }

    public static async Task DeleteFileFromBlobStorage(string fileName, BlobStorageContainers container)
    {
        BlobContainerClient containerClient = new(
            Secrets.AzureBlobStorageConnectionString,
            container.ToString());

        BlobClient blobClient = containerClient.GetBlobClient(fileName);

        await blobClient.DeleteIfExistsAsync();
    }

    public static async Task<List<string>> ListFilesInBlobStorage(BlobStorageContainers container)
    {
        BlobContainerClient containerClient = new(
            Secrets.AzureBlobStorageConnectionString,
            container.ToString());

        var fileUrls = new List<string>();

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
            fileUrls.Add(blobClient.Uri.ToString());
        }

        return fileUrls;
    }

    public static async Task<(MemoryStream fileStream, string contentType)> DownloadFileFromBlobStorage(string url, BlobStorageContainers container)
    {
        Uri blobUri = new(url);

        StorageSharedKeyCredential credentials = new(
            Secrets.AzureBlobStorageAccountName,
            Secrets.AzureBlobStorageAccountKey);

        BlobClient blobClient = new(blobUri, credentials);

        var downloadResponse = await blobClient.DownloadStreamingAsync();

        await using var memoryStream = new MemoryStream();
        await downloadResponse.Value.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return (memoryStream, downloadResponse.Value.Details.ContentType);
    }
}

public enum BlobStorageContainers
{
    purchase,
    purchasereturn
}