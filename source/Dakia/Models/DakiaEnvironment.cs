namespace Dakia.Models;

public class DakiaEnvironment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Environment";
    public List<EnvironmentVariable> Variables { get; set; } = [];
    public bool IsGlobal { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
