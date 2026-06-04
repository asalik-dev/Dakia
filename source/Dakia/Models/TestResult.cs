namespace Dakia.Models;

public class TestResult
{
    public string Name { get; set; } = "";
    public bool Passed { get; set; }
    public string? Error { get; set; }
    public long DurationMs { get; set; }
}
