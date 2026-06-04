namespace Dakia.Models;

public class RequestHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ApiRequest Request { get; set; } = new();
    public string RequestName { get; set; } = "";
    public ApiResponse? Response { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string? CollectionId { get; set; }
    public string? CollectionName { get; set; }

    public string DisplayName => string.IsNullOrEmpty(RequestName)
        ? $"{Request.Method} {TruncateUrl(Request.Url)}"
        : RequestName;

    private static string TruncateUrl(string url)
    {
        if (url.Length <= 60) return url;
        try
        {
            var uri = new Uri(url);
            return uri.AbsolutePath.Length > 40
                ? uri.AbsolutePath[..40] + "..."
                : uri.AbsolutePath;
        }
        catch
        {
            return url[..60] + "...";
        }
    }
}
