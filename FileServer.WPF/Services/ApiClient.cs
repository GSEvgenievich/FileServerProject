using FileServer.WPF.Models;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FileServer.WPF.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5000";

    public ApiClient()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    public async Task<List<FileItem>> GetFilesAsync()
    {
        var response = await _httpClient.GetStringAsync($"{BaseUrl}/api/files/list");
        return JsonSerializer.Deserialize<List<FileItem>>(response) ?? new();
    }

    public async Task<bool> UploadFileAsync(string filePath, Action<double>? progressCallback = null)
    {
        using var formData = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(filePath);
        using var streamContent = new ProgressableStreamContent(fileStream, 8192, progressCallback);

        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
        formData.Add(streamContent, "file", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync($"{BaseUrl}/api/files/upload", formData);
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]> GetFullFileAsync(Guid id)
    {
        return await _httpClient.GetByteArrayAsync($"{BaseUrl}/api/files/file/{id}");
    }

    public async Task DownloadAndSaveAsync(Guid id, string fileName, bool isImage)
    {
        var data = await _httpClient.GetByteArrayAsync($"{BaseUrl}/api/files/file/{id}");

        var folder = isImage ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Images")
                             : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Files");
        Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, fileName);
        await File.WriteAllBytesAsync(path, data);
    }

    public async Task DeleteFileAsync(Guid id)
    {
        await _httpClient.DeleteAsync($"{BaseUrl}/api/files/{id}");
    }

    private string GetMimeType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".pdf" => "application/pdf",
        ".txt" => "text/plain",
        ".zip" => "application/zip",
        _ => "application/octet-stream"
    };
}

public class ProgressableStreamContent : StreamContent
{
    private readonly Stream _stream;
    private readonly Action<double>? _progressCallback;
    private long _totalBytesRead;

    public ProgressableStreamContent(Stream stream, int bufferSize, Action<double>? progressCallback) : base(stream, bufferSize)
    {
        _stream = stream;
        _progressCallback = progressCallback;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var buffer = new byte[8192];
        var totalLength = _stream.Length;
        _totalBytesRead = 0;

        int bytesRead;
        while ((bytesRead = await _stream.ReadAsync(buffer)) > 0)
        {
            await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
            _totalBytesRead += bytesRead;
            _progressCallback?.Invoke((double)_totalBytesRead / totalLength * 100);
        }
    }
}