using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Configuration;
using System.Drawing;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace APSMain.Tcpip
{
    public class RestHelper
    {
        private static readonly Lazy<RestHelper> _instance = new(() => new RestHelper());
        public static RestHelper Instance => _instance.Value;

        private readonly HttpClient _client;
        private readonly string _apiKey;

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        private RestHelper()
        {
            string? baseUrl = ConfigurationManager.AppSettings["APIURI"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("AppSettings[APIURI]가 비어 있습니다.");

            _apiKey = ConfigurationManager.AppSettings["APIKEY"] ?? string.Empty;

            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 100
            };

            _client = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute),
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        private static CancellationTokenSource CreateTimeoutCts(TimeSpan timeout, CancellationToken ct)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);

            if (timeout != Timeout.InfiniteTimeSpan && timeout > TimeSpan.Zero)
                linked.CancelAfter(timeout);

            return linked;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path 가 비어 있습니다.", nameof(path));

            return path.TrimStart('/');
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var req = new HttpRequestMessage(method, NormalizePath(path));

            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(_apiKey))
                req.Headers.TryAddWithoutValidation("api-key", _apiKey);

            return req;
        }

        private static async Task<string> ReadResponseTextAsync(HttpResponseMessage res, CancellationToken ct)
        {
            return await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }

        public async Task<string?> PostAsync<TRequest>(string path, TRequest data, TimeSpan timeout = default, CancellationToken ct = default)
        {
            using var cts = CreateTimeoutCts(timeout == default ? TimeSpan.FromSeconds(10) : timeout, ct);

            string json = JsonConvert.SerializeObject(data, _jsonSettings);

            using var req = CreateRequest(HttpMethod.Post, path);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            string body = await ReadResponseTextAsync(res, cts.Token).ConfigureAwait(false);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException("POST 실패 : " + (int)res.StatusCode + " " + res.StatusCode + " / " + body);

            return body;
        }

        public async Task<string?> PostImageAsync(string path, string carNum, string imagePath, TimeSpan timeout = default, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                throw new FileNotFoundException("이미지 파일을 찾을 수 없습니다.", imagePath);

            using var cts = CreateTimeoutCts(timeout == default ? TimeSpan.FromSeconds(15) : timeout, ct);
            using var fs = File.OpenRead(imagePath);
            using var content = new MultipartFormDataContent();

            string fileName = Path.GetFileName(imagePath);
            string ext = Path.GetExtension(imagePath).ToLowerInvariant();
            string mediaType = ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };

            using var streamContent = new StreamContent(fs);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

            content.Add(streamContent, "image", fileName);
            content.Add(new StringContent(fileName), "filename");
            content.Add(new StringContent(carNum ?? string.Empty), "carnum");

            using var req = CreateRequest(HttpMethod.Post, path);
            req.Content = content;

            using var res = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            string body = await ReadResponseTextAsync(res, cts.Token).ConfigureAwait(false);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException("이미지 업로드 실패 : " + (int)res.StatusCode + " " + res.StatusCode + " / " + body);

            return body;
        }

        public async Task<Image?> GetImageAsync(string path, string fileName, TimeSpan timeout = default, CancellationToken ct = default)
        {
            using var cts = CreateTimeoutCts(timeout == default ? TimeSpan.FromSeconds(10) : timeout, ct);

            using var req = CreateRequest(HttpMethod.Post, path);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));
            req.Content = new StringContent(
                JsonConvert.SerializeObject(new { filename = fileName }, _jsonSettings),
                Encoding.UTF8,
                "application/json");

            using var res = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);

            if (!res.IsSuccessStatusCode) {
                string err = await ReadResponseTextAsync(res, cts.Token).ConfigureAwait(false);
                throw new HttpRequestException("이미지 다운로드 실패 : " + (int)res.StatusCode + " " + res.StatusCode + " / " + err);
            }

            await using var stream = await res.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cts.Token).ConfigureAwait(false);
            ms.Position = 0;

            using var img = Image.FromStream(ms, false, true);
            return (Image)img.Clone();
        }
    }
}
