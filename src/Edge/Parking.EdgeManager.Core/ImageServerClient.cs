namespace Parking.EdgeManager.Core;

public sealed class ImageServerClient
{
    private readonly HttpClient _httpClient;
    public ImageServerClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<byte[]?> DownloadAsync(
        string imageServerUrl,
        string? fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        Uri baseUri = new(imageServerUrl.TrimEnd('/') + "/", UriKind.Absolute);
        Uri requestUri = new(baseUri, $"api/image/download?fileName={Uri.EscapeDataString(fileName)}");
        using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        byte[] data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return data.Length == 0 ? null : data;
    }
}
