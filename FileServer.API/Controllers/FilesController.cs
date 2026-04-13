using FileServer.Domain.Entities;
using FileServer.Domain.Interfaces;
using FileServer.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileRepository _fileRepository;
    private readonly IStorageService _storageService;
    private readonly IThumbnailService _thumbnailService;
    private readonly IConfiguration _configuration;
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;

    public FilesController(
        IFileRepository fileRepository,
        IStorageService storageService,
        IThumbnailService thumbnailService,
        IConfiguration configuration)
    {
        _fileRepository = fileRepository;
        _storageService = storageService;
        _thumbnailService = thumbnailService;
        _configuration = configuration;

        _maxFileSize = configuration.GetValue<long>("FileSettings:MaxFileSizeMB") * 1024 * 1024;
        _allowedExtensions = configuration.GetSection("FileSettings:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>();
    }

    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Файл не выбран" });

        if (file.Length > _maxFileSize)
            return BadRequest(new { error = $"Максимальный размер: {_maxFileSize / 1024 / 1024} MB" });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return BadRequest(new { error = "Недопустимое расширение" });

        try
        {
            using var stream = file.OpenReadStream();

            // Загружаем в MinIO
            var storedName = await _storageService.UploadAsync(stream, file.FileName, file.ContentType, file.Length);

            // Создаем эскиз если это изображение
            byte[]? thumbnail = null;
            if (IsImageFile(file.ContentType))
            {
                stream.Position = 0;
                thumbnail = await _thumbnailService.CreateThumbnailAsync(stream);
            }

            var record = new FileRecord
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                StoredName = storedName,
                ContentType = file.ContentType,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow,
                Bucket = _configuration["Minio:BucketName"] ?? "uploads",
                Thumbnail = thumbnail
            };

            await _fileRepository.AddAsync(record);

            return Ok(new
            {
                id = record.Id,
                fileName = record.FileName,
                size = record.Size,
                contentType = record.ContentType,
                isImage = record.IsImage,
                thumbnail = thumbnail != null ? Convert.ToBase64String(thumbnail) : null
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetFileList()
    {
        var files = await _fileRepository.GetAllAsync();
        var result = files.Select(f => new
        {
            f.Id,
            f.FileName,
            f.Size,
            f.ContentType,
            f.UploadedAt,
            f.IsImage,
            Thumbnail = f.Thumbnail != null ? Convert.ToBase64String(f.Thumbnail) : null
        });

        return Ok(result);
    }

    [HttpGet("file/{id}")]
    public async Task<IActionResult> GetFullFile(Guid id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
            return NotFound();

        var stream = await _storageService.DownloadAsync(file.StoredName);
        return File(stream, file.ContentType, file.FileName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
            return NotFound();

        await _storageService.DeleteAsync(file.StoredName);
        await _fileRepository.DeleteAsync(id);

        return Ok(new { message = "Удалено" });
    }

    private bool IsImageFile(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}