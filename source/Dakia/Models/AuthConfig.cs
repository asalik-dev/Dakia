namespace Dakia.Models;

public class AuthConfig
{
    public string Type { get; set; } = "noauth";

    // Bearer Token
    public string BearerToken { get; set; } = "";

    // Basic Auth
    public string BasicUsername { get; set; } = "";
    public string BasicPassword { get; set; } = "";

    // API Key
    public string ApiKeyKey { get; set; } = "X-API-Key";
    public string ApiKeyValue { get; set; } = "";
    public string ApiKeyLocation { get; set; } = "header";

    // OAuth 1.0
    public string OAuth1ConsumerKey { get; set; } = "";
    public string OAuth1ConsumerSecret { get; set; } = "";
    public string OAuth1Token { get; set; } = "";
    public string OAuth1TokenSecret { get; set; } = "";
    public string OAuth1SignatureMethod { get; set; } = "HMAC-SHA1";
    public string OAuth1Version { get; set; } = "1.0";
    public string OAuth1Realm { get; set; } = "";
    public bool OAuth1AddParamsToHeader { get; set; } = true;

    // OAuth 2.0
    public string OAuth2Token { get; set; } = "";
    public string OAuth2TokenType { get; set; } = "Bearer";
    public string OAuth2AddTo { get; set; } = "header";
    public string OAuth2HeaderPrefix { get; set; } = "Bearer";
    public string OAuth2AuthUrl { get; set; } = "";
    public string OAuth2TokenUrl { get; set; } = "";
    public string OAuth2ClientId { get; set; } = "";
    public string OAuth2ClientSecret { get; set; } = "";
    public string OAuth2Scope { get; set; } = "";
    public string OAuth2GrantType { get; set; } = "authorization_code";

    // Digest Auth
    public string DigestUsername { get; set; } = "";
    public string DigestPassword { get; set; } = "";
    public string DigestRealm { get; set; } = "";
    public string DigestNonce { get; set; } = "";
    public string DigestAlgorithm { get; set; } = "MD5";
    public string DigestQop { get; set; } = "";

    // AWS Signature
    public string AwsAccessKey { get; set; } = "";
    public string AwsSecretKey { get; set; } = "";
    public string AwsRegion { get; set; } = "us-east-1";
    public string AwsService { get; set; } = "";
    public string AwsSessionToken { get; set; } = "";

    // JWT Bearer
    public string JwtSecret { get; set; } = "";
    public string JwtAlgorithm { get; set; } = "HS256";
    public string JwtPayload { get; set; } = "{\n  \"sub\": \"1234567890\",\n  \"iat\": 1516239022\n}";
    public string JwtHeaderPrefix { get; set; } = "Bearer";
    public bool JwtIsSecretBase64Encoded { get; set; } = false;
    public int JwtExpiresIn { get; set; } = 7200;

    // NTLM
    public string NtlmUsername { get; set; } = "";
    public string NtlmPassword { get; set; } = "";
    public string NtlmDomain { get; set; } = "";
    public string NtlmWorkstation { get; set; } = "";
}
