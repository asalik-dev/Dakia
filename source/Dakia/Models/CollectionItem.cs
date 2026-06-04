namespace Dakia.Models;

public class CollectionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Type { get; set; } = "request"; // "request" or "folder"
    public string Description { get; set; } = "";
    public List<CollectionItem> Items { get; set; } = [];
    public ApiRequest? Request { get; set; }
    public AuthConfig? Auth { get; set; }
    public string PreRequestScript { get; set; } = "";
    public string TestScript { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
