using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Api.Profile;

public sealed class ProfileMediaStorage(IConfiguration configuration, ILogger<ProfileMediaStorage> logger) : IDisposable
{
    private readonly string? bucket = configuration["ProfileMedia:Bucket"];
    private readonly IAmazonS3? client = CreateClient(configuration);

    private static IAmazonS3? CreateClient(IConfiguration configuration)
    {
        var settings = configuration.GetSection("ProfileMedia");
        if (settings["Endpoint"] is not { Length: > 0 } endpoint
            || settings["AccessKey"] is not { Length: > 0 } accessKey
            || settings["SecretKey"] is not { Length: > 0 } secretKey)
            return null;

        return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), new AmazonS3Config
        {
            ServiceURL = endpoint,
            AuthenticationRegion = "us-east-1",
            ForcePathStyle = true,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            MaxErrorRetry = 1,
            Timeout = TimeSpan.FromSeconds(10)
        });
    }

    public bool IsConfigured => client is not null && !string.IsNullOrWhiteSpace(bucket);

    public async Task<IResult> ReadAsync(string key, CancellationToken ct)
    {
        if (!IsConfigured)
            return Results.StatusCode(503);

        try
        {
            using var response = await client!.GetObjectAsync(bucket, key, ct);
            if (response.ContentLength is <= 0 or > ProfileImage.MaxFileSize)
                return Results.StatusCode(503);

            var bytes = new byte[(int)response.ContentLength];
            await response.ResponseStream.ReadExactlyAsync(bytes, ct);
            return Results.File(bytes, "image/webp");
        }
        catch (AmazonS3Exception exception) when (exception.ErrorCode == "NoSuchKey")
        {
            return Results.NotFound();
        }
        catch (Exception exception) when (IsStorageFailure(exception, ct))
        {
            logger.LogWarning(exception, "Could not read profile media");
            return Results.StatusCode(503);
        }
    }

    public async Task<IResult> WriteAsync(string key, byte[] bytes, CancellationToken ct)
    {
        if (!IsConfigured)
            return Results.StatusCode(503);

        using var stream = new MemoryStream(bytes, writable: false);
        try
        {
            await client!.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                ContentType = "image/webp",
                InputStream = stream,
                UseChunkEncoding = false
            }, ct);
            return Results.NoContent();
        }
        catch (Exception exception) when (IsStorageFailure(exception, ct))
        {
            logger.LogWarning(exception, "Could not write profile media");
            return Results.StatusCode(503);
        }
    }

    private static bool IsStorageFailure(Exception exception, CancellationToken ct) =>
        exception is AmazonServiceException or HttpRequestException or IOException or TimeoutException
        || exception is OperationCanceledException && !ct.IsCancellationRequested;

    public void Dispose() => client?.Dispose();
}
