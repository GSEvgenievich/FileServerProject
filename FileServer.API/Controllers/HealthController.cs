using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FileServer.Infrastructure.Data;
using FileServer.Infrastructure.Services;

namespace FileServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IStorageService _storageService;
    private readonly IConfiguration _configuration;

    public HealthController(
        AppDbContext dbContext,
        IStorageService storageService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> CheckHealth()
    {
        var health = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            services = new
            {
                api = "ok",
                database = await CheckDatabase(),
                storage = await CheckStorage()
            },
            configuration = new
            {
                maxFileSizeMB = _configuration.GetValue<long>("FileSettings:MaxFileSizeMB"),
                bucketName = _configuration["Minio:BucketName"]
            }
        };

        return Ok(health);
    }

    private async Task<string> CheckDatabase()
    {
        try
        {
            await _dbContext.Database.CanConnectAsync();
            return "ok";
        }
        catch
        {
            return "error";
        }
    }

    private async Task<string> CheckStorage()
    {
        try
        {
            // Пробуем загрузить тестовый объект
            var testData = System.Text.Encoding.UTF8.GetBytes("health-check");
            using var stream = new MemoryStream(testData);

            var testFile = await _storageService.UploadAsync(stream, "health.txt", "text/plain", testData.Length);
            await _storageService.DeleteAsync(testFile);

            return "ok";
        }
        catch
        {
            return "error";
        }
    }
}