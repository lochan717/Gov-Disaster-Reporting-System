using Microsoft.AspNetCore.Http;

namespace SAMVAD.DMS.Web.Services;

public interface IHttpService
{
    Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default);
    Task<byte[]?> GetBytesAsync(string url, CancellationToken cancellationToken = default);
    Task<T?> PostAsync<T>(string url, object payload, CancellationToken cancellationToken = default);
    Task<byte[]?> PostBytesAsync(string url, object payload, CancellationToken cancellationToken = default);
    Task<T?> PutAsync<T>(string url, object payload, CancellationToken cancellationToken = default);
    Task<T?> PatchAsync<T>(string url, object payload, CancellationToken cancellationToken = default);
    Task<T?> DeleteAsync<T>(string url, CancellationToken cancellationToken = default);
    Task<T?> PostMultipartAsync<T>(
        string url,
        IReadOnlyDictionary<string, string?> fields,
        IEnumerable<IFormFile>? files,
        string filesFieldName = "mediaFiles",
        CancellationToken cancellationToken = default);
}