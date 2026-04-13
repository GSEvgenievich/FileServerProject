using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace FileServer.Infrastructure.Services;

public interface IThumbnailService
{
    Task<byte[]> CreateThumbnailAsync(Stream imageStream, int maxWidth = 200, int quality = 60);
}

public class ThumbnailService : IThumbnailService
{
    public async Task<byte[]> CreateThumbnailAsync(Stream imageStream, int maxWidth = 200, int quality = 60)
    {
        using var image = await Image.LoadAsync(imageStream);

        // Изменяем размер
        if (image.Width > maxWidth)
        {
            var ratio = (double)maxWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(maxWidth, newHeight));
        }

        // Применяем blur для эффекта "мыльности"
        image.Mutate(x => x.GaussianBlur(3f));

        // Сохраняем в JPEG с низким качеством
        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = quality });

        return ms.ToArray();
    }
}