using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace MTS.Services;

public sealed class CloudflareR2Service : IDisposable
{
    private readonly CloudflareR2Options options;
    private readonly IAmazonS3 client;

    public CloudflareR2Service(IOptions<CloudflareR2Options> options)
    {
        this.options = options.Value;
        ValidateOptions(this.options);

        var credentials = new BasicAWSCredentials(
            this.options.AccessKeyId,
            this.options.SecretAccessKey);
        client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = $"https://{this.options.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
        });
    }

    public Task<R2UploadResult> UploadAsync(
        byte[] data,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        var stream = new MemoryStream(data, writable: false);
        return UploadAndDisposeAsync(stream, objectKey, contentType, cancellationToken);
    }

    public async Task<R2UploadResult> UploadAsync(
        Stream content,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(content));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));

        objectKey = NormalizeObjectKey(objectKey);
        var request = new PutObjectRequest
        {
            BucketName = options.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
        };

        var response = await client.PutObjectAsync(request, cancellationToken);
        return new R2UploadResult(
            objectKey,
            response.ETag,
            BuildPublicUrl(objectKey));
    }

    private async Task<R2UploadResult> UploadAndDisposeAsync(
        Stream content,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken)
    {
        await using (content)
        {
            return await UploadAsync(content, objectKey, contentType, cancellationToken);
        }
    }

    private string? BuildPublicUrl(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
            return null;

        var escapedKey = string.Join('/', objectKey
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
        return $"{options.PublicBaseUrl.TrimEnd('/')}/{escapedKey}";
    }

    private static string NormalizeObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Object key is required.", nameof(objectKey));

        return objectKey.Trim().TrimStart('/');
    }

    private static void ValidateOptions(CloudflareR2Options options)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(options.AccountId)) missing.Add(nameof(options.AccountId));
        if (string.IsNullOrWhiteSpace(options.AccessKeyId)) missing.Add(nameof(options.AccessKeyId));
        if (string.IsNullOrWhiteSpace(options.SecretAccessKey)) missing.Add(nameof(options.SecretAccessKey));
        if (string.IsNullOrWhiteSpace(options.BucketName)) missing.Add(nameof(options.BucketName));

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Missing Cloudflare R2 configuration: {string.Join(", ", missing)}.");
    }

    public void Dispose()
    {
        client.Dispose();
    }
}

