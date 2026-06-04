using Dakia.Models;
using Newtonsoft.Json;

namespace Dakia.Services;

public sealed class StorageService
{
    private static StorageService? _instance;
    public static StorageService Instance => _instance ??= new StorageService();

    public static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dakia");

    private readonly string _collectionsPath;
    private readonly string _environmentsPath;
    private readonly string _historyPath;
    private readonly string _settingsPath;

    private readonly JsonSerializerSettings _jsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    private List<RequestHistory>? _historyCache;

    private StorageService()
    {
        _collectionsPath = Path.Combine(AppDataPath, "collections");
        _environmentsPath = Path.Combine(AppDataPath, "environments");
        _historyPath = Path.Combine(AppDataPath, "history");
        _settingsPath = Path.Combine(AppDataPath, "settings.json");

        Directory.CreateDirectory(_collectionsPath);
        Directory.CreateDirectory(_environmentsPath);
        Directory.CreateDirectory(_historyPath);
    }

    // ── Collections ──────────────────────────────────────────────────────────

    public List<DakiaCollection> LoadAllCollections()
    {
        var collections = new List<DakiaCollection>();
        foreach (var file in Directory.GetFiles(_collectionsPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var col = JsonConvert.DeserializeObject<DakiaCollection>(json, _jsonSettings);
                if (col != null) collections.Add(col);
            }
            catch { /* skip corrupted files */ }
        }
        return [.. collections.OrderBy(c => c.Name)];
    }

    public void SaveCollection(DakiaCollection collection)
    {
        collection.UpdatedAt = DateTime.UtcNow;
        var path = Path.Combine(_collectionsPath, $"{SanitizeId(collection.Id)}.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(collection, _jsonSettings));
    }

    public void DeleteCollection(string id)
    {
        var path = Path.Combine(_collectionsPath, $"{SanitizeId(id)}.json");
        if (File.Exists(path)) File.Delete(path);
    }

    public DakiaCollection? LoadCollection(string id)
    {
        var path = Path.Combine(_collectionsPath, $"{SanitizeId(id)}.json");
        if (!File.Exists(path)) return null;
        try
        {
            return JsonConvert.DeserializeObject<DakiaCollection>(File.ReadAllText(path), _jsonSettings);
        }
        catch { return null; }
    }

    // ── Environments ─────────────────────────────────────────────────────────

    public List<DakiaEnvironment> LoadAllEnvironments()
    {
        var envs = new List<DakiaEnvironment>();
        foreach (var file in Directory.GetFiles(_environmentsPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var env = JsonConvert.DeserializeObject<DakiaEnvironment>(json, _jsonSettings);
                if (env != null) envs.Add(env);
            }
            catch { }
        }
        return [.. envs.OrderBy(e => e.Name)];
    }

    public void SaveEnvironment(DakiaEnvironment env)
    {
        env.UpdatedAt = DateTime.UtcNow;
        var path = Path.Combine(_environmentsPath, $"{SanitizeId(env.Id)}.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(env, _jsonSettings));
    }

    public void DeleteEnvironment(string id)
    {
        var path = Path.Combine(_environmentsPath, $"{SanitizeId(id)}.json");
        if (File.Exists(path)) File.Delete(path);
    }

    // ── History ───────────────────────────────────────────────────────────────

    public List<RequestHistory> LoadHistory()
    {
        if (_historyCache != null) return _historyCache;
        var path = Path.Combine(_historyPath, "history.json");
        if (!File.Exists(path)) { _historyCache = []; return _historyCache; }
        try
        {
            _historyCache = JsonConvert.DeserializeObject<List<RequestHistory>>(File.ReadAllText(path), _jsonSettings) ?? [];
        }
        catch { _historyCache = []; }
        return _historyCache;
    }

    public void AddToHistory(RequestHistory entry, int maxItems = 200)
    {
        var history = LoadHistory();
        history.Insert(0, entry);
        if (history.Count > maxItems) history.RemoveRange(maxItems, history.Count - maxItems);
        PersistHistory(history);
    }

    public void ClearHistory()
    {
        _historyCache = [];
        PersistHistory(_historyCache);
    }

    public void InvalidateHistoryCache() => _historyCache = null;

    private void PersistHistory(List<RequestHistory> history)
    {
        File.WriteAllText(
            Path.Combine(_historyPath, "history.json"),
            JsonConvert.SerializeObject(history, _jsonSettings));
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    public WorkspaceSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath)) return new WorkspaceSettings();
        try
        {
            return JsonConvert.DeserializeObject<WorkspaceSettings>(File.ReadAllText(_settingsPath), _jsonSettings)
                   ?? new WorkspaceSettings();
        }
        catch { return new WorkspaceSettings(); }
    }

    public void SaveSettings(WorkspaceSettings settings)
    {
        File.WriteAllText(_settingsPath, JsonConvert.SerializeObject(settings, _jsonSettings));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string SanitizeId(string id) =>
        string.Concat(id.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));

    public string GetDataDirectory() => AppDataPath;

    public void ExportCollection(DakiaCollection collection, string filePath)
    {
        File.WriteAllText(filePath, JsonConvert.SerializeObject(collection, _jsonSettings));
    }

    public DakiaCollection? ImportCollection(string filePath)
    {
        try
        {
            return JsonConvert.DeserializeObject<DakiaCollection>(File.ReadAllText(filePath), _jsonSettings);
        }
        catch { return null; }
    }
}
