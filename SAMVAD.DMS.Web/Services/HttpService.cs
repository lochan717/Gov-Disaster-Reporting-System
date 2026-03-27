using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace SAMVAD.DMS.Web.Services;

public class HttpService : IHttpService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, url, null, cancellationToken);

    public Task<byte[]?> GetBytesAsync(string url, CancellationToken cancellationToken = default) =>
        SendBytesAsync(HttpMethod.Get, url, null, cancellationToken);

    public Task<T?> PostAsync<T>(string url, object payload, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Post, url, payload, cancellationToken);

    public Task<byte[]?> PostBytesAsync(string url, object payload, CancellationToken cancellationToken = default) =>
        SendBytesAsync(HttpMethod.Post, url, payload, cancellationToken);

    public Task<T?> PutAsync<T>(string url, object payload, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Put, url, payload, cancellationToken);

    public Task<T?> PatchAsync<T>(string url, object payload, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Patch, url, payload, cancellationToken);

    public Task<T?> DeleteAsync<T>(string url, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Delete, url, null, cancellationToken);

    public async Task<T?> PostMultipartAsync<T>(
        string url,
        IReadOnlyDictionary<string, string?> fields,
        IEnumerable<IFormFile>? files,
        string filesFieldName = "mediaFiles",
        CancellationToken cancellationToken = default)
    {
        using var request = await BuildMultipartRequestAsync(HttpMethod.Post, url, fields, files, filesFieldName);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(responseText))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(responseText, SerializerOptions);
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string url, object? payload, CancellationToken cancellationToken)
    {
        using var request = await BuildRequestAsync(method, url, payload);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(responseText))
        {
            return default;
        }

        // Deserialize both success and error payloads so callers can surface server-side messages.
        return JsonSerializer.Deserialize<T>(responseText, SerializerOptions);
    }

    private async Task<byte[]?> SendBytesAsync(HttpMethod method, string url, object? payload, CancellationToken cancellationToken)
    {
        using var request = await BuildRequestAsync(method, url, payload);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(HttpMethod method, string url, object? payload)
    {
        var request = new HttpRequestMessage(method, url);

        string? token = null;
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var authResult = await httpContext.AuthenticateAsync();
            if (authResult.Succeeded)
            {
                authResult.Properties?.Items.TryGetValue("access_token", out token);
            }
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (payload is not null)
        {
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private async Task<HttpRequestMessage> BuildMultipartRequestAsync(
        HttpMethod method,
        string url,
        IReadOnlyDictionary<string, string?> fields,
        IEnumerable<IFormFile>? files,
        string filesFieldName)
    {
        var request = new HttpRequestMessage(method, url);

        string? token = null;
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var authResult = await httpContext.AuthenticateAsync();
            if (authResult.Succeeded)
            {
                authResult.Properties?.Items.TryGetValue("access_token", out token);
            }
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var content = new MultipartFormDataContent();
        foreach (var field in fields)
        {
            content.Add(new StringContent(field.Value ?? string.Empty), field.Key);
        }

        if (files is not null)
        {
            foreach (var file in files)
            {
                if (file is null || file.Length == 0)
                {
                    continue;
                }

                var streamContent = new StreamContent(file.OpenReadStream());
                if (!string.IsNullOrWhiteSpace(file.ContentType))
                {
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                }

                content.Add(streamContent, filesFieldName, file.FileName);
            }
        }

        request.Content = content;
        return request;
    }
}