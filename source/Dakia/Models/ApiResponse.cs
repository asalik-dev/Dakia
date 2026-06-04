using System.Net;

namespace Dakia.Models;

public class ApiResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public string StatusText { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = [];
    public string Body { get; set; } = "";
    public byte[]? BinaryBody { get; set; }
    public long ResponseTimeMs { get; set; }
    public long ResponseSizeBytes { get; set; }
    public string ContentType { get; set; } = "";
    public List<TestResult> TestResults { get; set; } = [];
    public List<string> ConsoleLog { get; set; } = [];
    public string? ErrorMessage { get; set; }
    public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
