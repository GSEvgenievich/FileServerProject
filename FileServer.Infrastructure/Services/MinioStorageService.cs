using Minio;
using Minio.DataModel.Args;

namespace FileServer.Infrastructure.Services;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public MinioStorageService(IMinioClient minioClient, string bucketName)
    {
        _minioClient = minioClient;
        _bucketName = bucketName;
        EnsureBucketExistsAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureBucketExistsAsync()
    {
        bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
        if (!found)
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, long size)
    {
        var objectName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(size)
            .WithContentType(contentType));

        return objectName;
    }

    public async Task<Stream> DownloadAsync(string objectName)
    {
        var ms = new MemoryStream();
        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithCallbackStream(s => s.CopyTo(ms)));

        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string objectName)
    {
        await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName));
    }
}