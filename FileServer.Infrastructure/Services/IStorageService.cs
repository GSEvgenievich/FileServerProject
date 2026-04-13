namespace FileServer.Infrastructure.Services;

public interface IStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, long size);
    Task<Stream> DownloadAsync(string objectName);
    Task DeleteAsync(string objectName);
}