using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace BpmnProperties.Infrastructure;

public interface IObjectStorage
{
    Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);
    Task<Stream> GetAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

public sealed class S3ObjectStorage(IAmazonS3 client, IConfiguration configuration) : IObjectStorage
{
    private readonly string _bucket = configuration["Storage:Bucket"] ?? "bpmn-properties";

    public Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken) =>
        client.PutObjectAsync(new PutObjectRequest { BucketName = _bucket, Key = key, InputStream = content, ContentType = contentType }, cancellationToken);

    public async Task<Stream> GetAsync(string key, CancellationToken cancellationToken)
    {
        var response = await client.GetObjectAsync(_bucket, key, cancellationToken);
        var memory = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        return memory;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        client.DeleteObjectAsync(_bucket, key, cancellationToken);
}
