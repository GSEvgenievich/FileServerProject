using FileServer.Domain.Interfaces;
using FileServer.Infrastructure.Data;
using FileServer.Infrastructure.Repositories;
using FileServer.Infrastructure.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Minio;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWpf", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Размер запроса
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 104857600;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 104857600;
});

// Контроллеры и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "File Server API",
        Version = "v1",
        Description = "API для файлового сервера"
    });
});

// Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MinIO
var minioConfig = builder.Configuration.GetSection("Minio");
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    return new MinioClient()
        .WithEndpoint(minioConfig["Endpoint"])
        .WithCredentials(minioConfig["AccessKey"], minioConfig["SecretKey"])
        .WithSSL(minioConfig.GetValue<bool>("UseSsl"))
        .Build();
});

// 🔥 ВАЖНО: Регистрация сервисов
builder.Services.AddScoped<IStorageService>(sp =>
{
    var minioClient = sp.GetRequiredService<IMinioClient>();
    var bucketName = minioConfig["BucketName"] ?? "uploads";
    return new MinioStorageService(minioClient, bucketName);
});

// 🔥 РЕГИСТРАЦИЯ IThumbnailService
builder.Services.AddScoped<IThumbnailService, ThumbnailService>();
builder.Services.AddScoped<IFileRepository, FileRepository>();

var app = builder.Build();

// Создание БД
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var created = dbContext.Database.EnsureCreated();

    if (created)
    {
        Console.WriteLine("✅ База данных создана успешно");
    }
    else
    {
        Console.WriteLine("✅ База данных уже существует");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "File Server API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowWpf");
app.UseAuthorization();
app.MapControllers();

app.Run();