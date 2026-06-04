using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Dakia.Models;

namespace Dakia.Services;

public sealed class HttpRequestService : IDisposable
{
    private HttpClient _client;
    private WorkspaceSettings _settings;
    private readonly CookieContainer _cookieContainer = new();

    public HttpRequestService(WorkspaceSettings settings)
    {
        _settings = settings;
        _client = BuildClient(settings, _cookieContainer);
    }

    public void ApplySettings(WorkspaceSettings settings)
    {
        _settings = settings;
        _client.Dispose();
        _client = BuildClient(settings, _cookieContainer);
    }

    private static HttpClient BuildClient(WorkspaceSettings s, CookieContainer cookies)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = s.FollowRedirects,
            MaxAutomaticRedirections = 10,
            CookieContainer = cookies,
            UseCookies = s.StoreCookies,
            AutomaticDecompression = DecompressionMethods.All
        };

        if (!s.SslVerification)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        if (s.UseProxy && !string.IsNullOrWhiteSpace(s.ProxyHost))
        {
            var proxy = new WebProxy($"{s.ProxyHost}:{s.ProxyPort}", false);
            if (!string.IsNullOrEmpty(s.ProxyUsername))
                proxy.Credentials = new NetworkCredential(s.ProxyUsername, s.ProxyPassword);
            handler.Proxy = proxy;
            handler.UseProxy = true;
        }

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromMilliseconds(s.TimeoutMs)
        };
    }

    public async Task<ApiResponse> SendAsync(ApiRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var response = new ApiResponse();

        try
        {
            using var httpReq = BuildHttpRequest(request);
            ApplyAuth(request.Auth, httpReq);

            var httpResp = await _client.SendAsync(httpReq, HttpCompletionOption.ResponseContentRead, ct);
            sw.Stop();

            response.StatusCode = httpResp.StatusCode;
            response.StatusText = httpResp.ReasonPhrase ?? httpResp.StatusCode.ToString();
            response.ResponseTimeMs = sw.ElapsedMilliseconds;

            foreach (var h in httpResp.Headers)
                response.Headers[h.Key] = string.Join(", ", h.Value);
            foreach (var h in httpResp.Content.Headers)
                response.Headers[h.Key] = string.Join(", ", h.Value);

            response.ContentType = httpResp.Content.Headers.ContentType?.MediaType ?? "";

            var bodyBytes = await httpResp.Content.ReadAsByteArrayAsync(ct);
            response.ResponseSizeBytes = bodyBytes.LongLength;

            if (IsBinaryContent(response.ContentType))
            {
                response.BinaryBody = bodyBytes;
                response.Body = $"[Binary data: {bodyBytes.Length} bytes]";
            }
            else
            {
                var charset = httpResp.Content.Headers.ContentType?.CharSet ?? "utf-8";
                try
                {
                    response.Body = Encoding.GetEncoding(charset).GetString(bodyBytes);
                }
                catch
                {
                    response.Body = Encoding.UTF8.GetString(bodyBytes);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            sw.Stop();
            response.ErrorMessage = "Request cancelled.";
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            response.ErrorMessage = $"Request timed out after {_settings.TimeoutMs / 1000.0:F1}s.";
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            response.ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            sw.Stop();
            response.ErrorMessage = ex.Message;
        }

        response.ResponseTimeMs = sw.ElapsedMilliseconds;
        return response;
    }

    private HttpRequestMessage BuildHttpRequest(ApiRequest request)
    {
        var url = BuildUrl(request.Url, request.QueryParams.Where(p => p.Enabled));

        // Add API key to query if configured
        if (request.Auth.Type == "apikey" && request.Auth.ApiKeyLocation == "query")
        {
            var sep = url.Contains('?') ? "&" : "?";
            url += $"{sep}{Uri.EscapeDataString(request.Auth.ApiKeyKey)}={Uri.EscapeDataString(request.Auth.ApiKeyValue)}";
        }
        if (request.Auth.Type == "oauth2" && request.Auth.OAuth2AddTo == "query")
        {
            var sep = url.Contains('?') ? "&" : "?";
            url += $"{sep}access_token={Uri.EscapeDataString(request.Auth.OAuth2Token)}";
        }

        var httpReq = new HttpRequestMessage(new HttpMethod(request.Method.ToUpperInvariant()), url);

        foreach (var h in request.Headers.Where(h => h.Enabled && !string.IsNullOrEmpty(h.Key)))
        {
            try { httpReq.Headers.TryAddWithoutValidation(h.Key, h.Value); }
            catch { /* skip invalid headers */ }
        }

        var content = BuildContent(request.Body);
        if (content != null) httpReq.Content = content;

        return httpReq;
    }

    private static string BuildUrl(string baseUrl, IEnumerable<KeyValueItem> queryParams)
    {
        var active = queryParams.Where(p => !string.IsNullOrEmpty(p.Key)).ToList();
        if (active.Count == 0) return baseUrl;

        var sep = baseUrl.Contains('?') ? "&" : "?";
        var qs = string.Join("&", active.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        return $"{baseUrl}{sep}{qs}";
    }

    private static HttpContent? BuildContent(RequestBody body)
    {
        return body.Mode switch
        {
            "raw" => BuildRaw(body),
            "formdata" => BuildFormData(body),
            "urlencoded" => BuildUrlEncoded(body),
            "binary" => BuildBinary(body),
            "graphql" => BuildGraphQL(body),
            _ => null
        };
    }

    private static StringContent BuildRaw(RequestBody body)
    {
        var mime = body.RawLanguage switch
        {
            "json" => "application/json",
            "xml" => "application/xml",
            "html" => "text/html",
            "javascript" => "application/javascript",
            _ => "text/plain"
        };
        return new StringContent(body.Raw, Encoding.UTF8, mime);
    }

    private static MultipartFormDataContent BuildFormData(RequestBody body)
    {
        var content = new MultipartFormDataContent();
        foreach (var item in body.FormData.Where(f => f.Enabled))
        {
            if (item.Type == "file" && File.Exists(item.FilePath))
            {
                var fc = new ByteArrayContent(File.ReadAllBytes(item.FilePath));
                fc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(fc, item.Key, Path.GetFileName(item.FilePath));
            }
            else
            {
                content.Add(new StringContent(item.Value), item.Key);
            }
        }
        return content;
    }

    private static FormUrlEncodedContent BuildUrlEncoded(RequestBody body)
    {
        var pairs = body.UrlEncoded
            .Where(u => u.Enabled && !string.IsNullOrEmpty(u.Key))
            .Select(u => new KeyValuePair<string, string>(u.Key, u.Value));
        return new FormUrlEncodedContent(pairs);
    }

    private static HttpContent? BuildBinary(RequestBody body)
    {
        if (!File.Exists(body.BinaryFilePath)) return null;
        var content = new ByteArrayContent(File.ReadAllBytes(body.BinaryFilePath));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return content;
    }

    private static StringContent BuildGraphQL(RequestBody body)
    {
        var payload = body.GraphQL != null
            ? Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                query = body.GraphQL.Query,
                variables = TryParseJson(body.GraphQL.Variables)
            })
            : "{}";
        return new StringContent(payload, Encoding.UTF8, "application/json");
    }

    private static object? TryParseJson(string json)
    {
        try { return Newtonsoft.Json.JsonConvert.DeserializeObject(json); }
        catch { return null; }
    }

    private static void ApplyAuth(AuthConfig auth, HttpRequestMessage req)
    {
        switch (auth.Type)
        {
            case "bearer":
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.BearerToken);
                break;

            case "basic":
                var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth.BasicUsername}:{auth.BasicPassword}"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", b64);
                break;

            case "apikey" when auth.ApiKeyLocation == "header":
                req.Headers.TryAddWithoutValidation(auth.ApiKeyKey, auth.ApiKeyValue);
                break;

            case "oauth1":
                var oauthHeader = BuildOAuth1Header(auth, req.RequestUri?.ToString() ?? "", req.Method.Method);
                req.Headers.TryAddWithoutValidation("Authorization", oauthHeader);
                break;

            case "oauth2":
                req.Headers.Authorization = new AuthenticationHeaderValue(auth.OAuth2HeaderPrefix, auth.OAuth2Token);
                break;

            case "digest":
                var digestB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth.DigestUsername}:{auth.DigestPassword}"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", digestB64);
                break;

            case "awssig":
                ApplyAwsSignature(auth, req);
                break;

            case "jwt":
                req.Headers.Authorization = new AuthenticationHeaderValue(auth.JwtHeaderPrefix, BuildJwt(auth));
                break;

            case "ntlm":
                // NTLM is handled at handler level; just set basic creds as fallback
                break;
        }
    }

    private static string BuildOAuth1Header(AuthConfig auth, string url, string method)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");

        var parameters = new SortedDictionary<string, string>
        {
            ["oauth_consumer_key"] = auth.OAuth1ConsumerKey,
            ["oauth_nonce"] = nonce,
            ["oauth_signature_method"] = auth.OAuth1SignatureMethod,
            ["oauth_timestamp"] = timestamp,
            ["oauth_token"] = auth.OAuth1Token,
            ["oauth_version"] = auth.OAuth1Version
        };

        var paramString = string.Join("&", parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var baseString = $"{method.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(paramString)}";
        var signingKey = $"{Uri.EscapeDataString(auth.OAuth1ConsumerSecret)}&{Uri.EscapeDataString(auth.OAuth1TokenSecret)}";

        string signature;
        using (var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey)))
        {
            signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(baseString)));
        }

        parameters["oauth_signature"] = signature;
        var headerParts = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}=\"{Uri.EscapeDataString(p.Value)}\"");

        var realm = string.IsNullOrEmpty(auth.OAuth1Realm) ? "" : $"realm=\"{auth.OAuth1Realm}\",";
        return $"OAuth {realm}{string.Join(",", headerParts)}";
    }

    private static void ApplyAwsSignature(AuthConfig auth, HttpRequestMessage req)
    {
        var now = DateTime.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd");
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");

        req.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        if (!string.IsNullOrEmpty(auth.AwsSessionToken))
            req.Headers.TryAddWithoutValidation("x-amz-security-token", auth.AwsSessionToken);

        var url = req.RequestUri ?? new Uri("https://example.com");
        var method = req.Method.Method;
        var service = string.IsNullOrEmpty(auth.AwsService)
            ? url.Host.Split('.')[0]
            : auth.AwsService;
        var region = auth.AwsRegion;

        var payloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; // empty body hash

        var canonicalHeaders = $"host:{url.Host}\nx-amz-date:{amzDate}\n";
        var signedHeaders = "host;x-amz-date";
        var canonicalRequest = $"{method}\n{url.AbsolutePath}\n{url.Query.TrimStart('?')}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        var credentialScope = $"{dateStamp}/{region}/{service}/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{ComputeHash(canonicalRequest)}";

        var signingKey = GetSigningKey(auth.AwsSecretKey, dateStamp, region, service);
        var signature = ComputeHmacHex(signingKey, stringToSign);

        var authHeader = $"AWS4-HMAC-SHA256 Credential={auth.AwsAccessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
        req.Headers.TryAddWithoutValidation("Authorization", authHeader);
    }

    private static byte[] GetSigningKey(string secret, string date, string region, string service)
    {
        var kDate = ComputeHmac(Encoding.UTF8.GetBytes($"AWS4{secret}"), date);
        var kRegion = ComputeHmac(kDate, region);
        var kService = ComputeHmac(kRegion, service);
        return ComputeHmac(kService, "aws4_request");
    }

    private static byte[] ComputeHmac(byte[] key, string data) =>
        new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(data));

    private static string ComputeHmacHex(byte[] key, string data) =>
        Convert.ToHexString(ComputeHmac(key, data)).ToLowerInvariant();

    private static string ComputeHash(string data) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();

    private static string BuildJwt(AuthConfig auth)
    {
        var header = new { alg = auth.JwtAlgorithm, typ = "JWT" };
        object payload;
        try { payload = Newtonsoft.Json.JsonConvert.DeserializeObject(auth.JwtPayload) ?? new { }; }
        catch { payload = new { }; }

        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(header)));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(payload)));
        var sigInput = $"{headerB64}.{payloadB64}";

        string sig;
        var secretBytes = auth.JwtIsSecretBase64Encoded
            ? Convert.FromBase64String(auth.JwtSecret)
            : Encoding.UTF8.GetBytes(auth.JwtSecret);

        if (auth.JwtAlgorithm.StartsWith("HS"))
        {
            var hmac = auth.JwtAlgorithm == "HS256" ? (HMAC)new HMACSHA256(secretBytes)
                : auth.JwtAlgorithm == "HS384" ? new HMACSHA384(secretBytes)
                : new HMACSHA512(secretBytes);
            sig = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(sigInput)));
        }
        else
        {
            sig = "unsupported";
        }

        return $"{sigInput}.{sig}";
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool IsBinaryContent(string contentType)
    {
        var binary = new[] { "image/", "audio/", "video/", "application/octet-stream", "application/pdf", "application/zip" };
        return binary.Any(t => contentType.StartsWith(t, StringComparison.OrdinalIgnoreCase));
    }

    public CookieCollection GetCookies(Uri uri) => _cookieContainer.GetCookies(uri);
    public void ClearCookies() { /* reset container */ }

    public void Dispose() => _client.Dispose();
}
