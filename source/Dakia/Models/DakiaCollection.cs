namespace Dakia.Models;

public class DakiaCollection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Collection";
    public string Description { get; set; } = "";
    public List<CollectionItem> Items { get; set; } = [];
    public List<EnvironmentVariable> Variables { get; set; } = [];
    public AuthConfig Auth { get; set; } = new();
    public string PreRequestScript { get; set; } = "";
    public string TestScript { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class EnvironmentVariable
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string InitialValue { get; set; } = "";
    public string Type { get; set; } = "text";
    public bool Enabled { get; set; } = true;
    public string Description { get; set; } = "";
}
