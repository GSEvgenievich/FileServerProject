using System.Collections.Concurrent;
using System.Net.Http;

namespace FileServer.WPF.Services;

public class ImageCacheService
{
    private readonly ConcurrentDictionary<Guid, byte[]> _imageCache = new();
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore = new(4, 4);
    private const string BaseUrl = "http://localhost:5000";

    public ImageCacheService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<byte[]?> GetImageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Проверяем кэш
        if (_imageCache.TryGetValue(id, out var cached))
            return cached;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Двойная проверка кэша
            if (_imageCache.TryGetValue(id, out cached))
                return cached;

            // 🔥 Используем preview эндпоинт (который точно работает)
            var url = $"{BaseUrl}/api/files/preview/{id}";
            var data = await _httpClient.GetByteArrayAsync(url, cancellationToken);

            _imageCache[id] = data;
            return data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки {id}: {ex.Message}");
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Clear() => _imageCache.Clear();

    public void Remove(Guid id) => _imageCache.TryRemove(id, out _);
}