namespace Dakia.Models;

public class ApiRequest
{
    public string Url { get; set; } = "";
    public string Method { get; set; } = "GET";
    public List<KeyValueItem> Headers { get; set; } = [];
    public List<KeyValueItem> QueryParams { get; set; } = [];
    public RequestBody Body { get; set; } = new();
    public AuthConfig Auth { get; set; } = new();
    public string PreRequestScript { get; set; } = "";
    public string TestScript { get; set; } = "";
}

public class KeyValueItem
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

public class RequestBody
{
    public string Mode { get; set; } = "none";
    public string Raw { get; set; } = "";
    public string RawLanguage { get; set; } = "json";
    public List<FormDataItem> FormData { get; set; } = [];
    public List<KeyValueItem> UrlEncoded { get; set; } = [];
    public string BinaryFilePath { get; set; } = "";
    public GraphQLBody? GraphQL { get; set; }
}

public class FormDataItem
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Type { get; set; } = "text";
    public string FilePath { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

public class GraphQLBody
{
    public string Query { get; set; } = "";
    public string Variables { get; set; } = "{}";
}
