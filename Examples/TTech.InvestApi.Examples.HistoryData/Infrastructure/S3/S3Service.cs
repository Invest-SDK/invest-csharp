using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace TTech.InvestApi.Examples.HistoryData.Infrastructure.S3;

public interface IS3Service
{
    Task<bool> CheckIfFileExists(string bucketName, string fileName, CancellationToken cancellationToken);
    Task<Stream> Download(string bucketName, string fileName, CancellationToken cancellationToken);
    Task Upload(Stream stream, string fileName, string bucketName, string contentType, CancellationToken cancellationToken);
}

public class S3Service(IAmazonS3 client) : IS3Service
{
    public async Task<bool> CheckIfFileExists(string bucketName, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            await client.GetObjectMetadataAsync(bucketName, fileName, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<Stream> Download(string bucketName, string fileName, CancellationToken cancellationToken)
    {
        var request = new GetObjectRequest()
        {
            BucketName = bucketName,
            Key = fileName
        };

        var response = await client.GetObjectAsync(request, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException("S3 response status code is not 200", null, response.HttpStatusCode);
        }

        return response.ResponseStream;

    }

    public async Task Upload(Stream stream, string fileName, string bucketName, string contentType, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        var request = new PutObjectRequest()
        {
            BucketName = bucketName,
            Key = fileName,
            ContentType = contentType,
            InputStream = stream
        };

        var response = await client.PutObjectAsync(request, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException("S3 response status code is not 200", null, response.HttpStatusCode);
        }
    }
}
