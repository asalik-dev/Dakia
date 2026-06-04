using Dakia.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dakia.Services;

public sealed class ImportExportService
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };

    // ── Export to Postman Collection v2.1 ────────────────────────────────────

    public string ExportToPostmanV21(DakiaCollection collection)
    {
        var obj = new JObject
        {
            ["info"] = new JObject
            {
                ["_postman_id"] = collection.Id,
                ["name"] = collection.Name,
                ["description"] = collection.Description,
                ["schema"] = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            ["item"] = BuildPostmanItems(collection.Items),
            ["variable"] = BuildPostmanVariables(collection.Variables),
            ["auth"] = BuildPostmanAuth(collection.Auth),
            ["event"] = BuildPostmanEvents(collection.PreRequestScript, collection.TestScript)
        };
        return obj.ToString(Formatting.Indented);
    }

    private static JArray BuildPostmanItems(List<CollectionItem> items)
    {
        var arr = new JArray();
        foreach (var item in items)
        {
            if (item.Type == "folder")
            {
                arr.Add(new JObject
                {
                    ["name"] = item.Name,
                    ["description"] = item.Description,
                    ["item"] = BuildPostmanItems(item.Items),
                    ["auth"] = item.Auth != null ? BuildPostmanAuth(item.Auth) : null,
                    ["event"] = BuildPostmanEvents(item.PreRequestScript, item.TestScript)
                });
            }
            else if (item.Request != null)
            {
                arr.Add(BuildPostmanRequest(item));
            }
        }
        return arr;
    }

    private static JObject BuildPostmanRequest(CollectionItem item)
    {
        var req = item.Request!;
        var urlStr = req.Url;
        JToken urlToken;

        try
        {
            var uri = new Uri(urlStr.Contains("{{") ? "https://example.com/placeholder" : urlStr);
            urlToken = new JObject
            {
                ["raw"] = urlStr,
                ["protocol"] = uri.Scheme,
                ["host"] = new JArray(uri.Host.Split('.')),
                ["path"] = new JArray(uri.AbsolutePath.Trim('/').Split('/').Where(s => !string.IsNullOrEmpty(s))),
                ["query"] = BuildPostmanKeyValues(req.QueryParams)
            };
        }
        catch
        {
            urlToken = urlStr;
        }

        return new JObject
        {
            ["name"] = item.Name,
            ["description"] = item.Description,
            ["event"] = BuildPostmanEvents(req.PreRequestScript, req.TestScript),
            ["request"] = new JObject
            {
                ["method"] = req.Method,
                ["header"] = BuildPostmanKeyValues(req.Headers),
                ["url"] = urlToken,
                ["auth"] = BuildPostmanAuth(req.Auth),
                ["body"] = BuildPostmanBody(req.Body)
            }
        };
    }

    private static JArray BuildPostmanKeyValues(List<KeyValueItem> items)
    {
        var arr = new JArray();
        foreach (var item in items)
            arr.Add(new JObject
            {
                ["key"] = item.Key,
                ["value"] = item.Value,
                ["description"] = item.Description,
                ["disabled"] = !item.Enabled
            });
        return arr;
    }

    private static JObject? BuildPostmanBody(RequestBody body)
    {
        if (body.Mode == "none") return null;
        var obj = new JObject { ["mode"] = body.Mode };
        switch (body.Mode)
        {
            case "raw":
                obj["raw"] = body.Raw;
                obj["options"] = new JObject { ["raw"] = new JObject { ["language"] = body.RawLanguage } };
                break;
            case "formdata":
                var fd = new JArray();
                foreach (var f in body.FormData)
                    fd.Add(new JObject
                    {
                        ["key"] = f.Key, ["value"] = f.Value, ["type"] = f.Type,
                        ["src"] = f.Type == "file" ? f.FilePath : null,
                        ["disabled"] = !f.Enabled
                    });
                obj["formdata"] = fd;
                break;
            case "urlencoded":
                obj["urlencoded"] = BuildPostmanKeyValues(body.UrlEncoded);
                break;
            case "graphql":
                obj["graphql"] = new JObject
                {
                    ["query"] = body.GraphQL?.Query ?? "",
                    ["variables"] = body.GraphQL?.Variables ?? "{}"
                };
                break;
        }
        return obj;
    }

    private static JObject? BuildPostmanAuth(AuthConfig auth)
    {
        if (auth.Type == "noauth") return new JObject { ["type"] = "noauth" };
        var obj = new JObject { ["type"] = auth.Type };
        switch (auth.Type)
        {
            case "bearer":
                obj["bearer"] = new JArray(new JObject { ["key"] = "token", ["value"] = auth.BearerToken, ["type"] = "string" });
                break;
            case "basic":
                obj["basic"] = new JArray(
                    new JObject { ["key"] = "username", ["value"] = auth.BasicUsername, ["type"] = "string" },
                    new JObject { ["key"] = "password", ["value"] = auth.BasicPassword, ["type"] = "string" });
                break;
            case "apikey":
                obj["apikey"] = new JArray(
                    new JObject { ["key"] = "key", ["value"] = auth.ApiKeyKey, ["type"] = "string" },
                    new JObject { ["key"] = "value", ["value"] = auth.ApiKeyValue, ["type"] = "string" },
                    new JObject { ["key"] = "in", ["value"] = auth.ApiKeyLocation, ["type"] = "string" });
                break;
        }
        return obj;
    }

    private static JArray BuildPostmanEvents(string preReq, string test)
    {
        var arr = new JArray();
        if (!string.IsNullOrWhiteSpace(preReq))
            arr.Add(new JObject
            {
                ["listen"] = "prerequest",
                ["script"] = new JObject
                {
                    ["type"] = "text/javascript",
                    ["exec"] = new JArray(preReq.Split('\n'))
                }
            });
        if (!string.IsNullOrWhiteSpace(test))
            arr.Add(new JObject
            {
                ["listen"] = "test",
                ["script"] = new JObject
                {
                    ["type"] = "text/javascript",
                    ["exec"] = new JArray(test.Split('\n'))
                }
            });
        return arr;
    }

    private static JArray BuildPostmanVariables(List<EnvironmentVariable> vars)
    {
        var arr = new JArray();
        foreach (var v in vars)
            arr.Add(new JObject
            {
                ["key"] = v.Key,
                ["value"] = v.Value,
                ["type"] = v.Type,
                ["disabled"] = !v.Enabled
            });
        return arr;
    }

    // ── Import from Postman Collection v2 / v2.1 ─────────────────────────────

    public DakiaCollection? ImportPostmanCollection(string json)
    {
        try
        {
            var jObj = JObject.Parse(json);
            var info = jObj["info"];
            if (info == null) return null;

            var col = new DakiaCollection
            {
                Id = info["_postman_id"]?.ToString() ?? Guid.NewGuid().ToString(),
                Name = info["name"]?.ToString() ?? "Imported Collection",
                Description = info["description"]?.ToString() ?? ""
            };

            var items = jObj["item"] as JArray;
            if (items != null) col.Items = ParsePostmanItems(items);

            var vars = jObj["variable"] as JArray;
            if (vars != null) col.Variables = ParsePostmanVariables(vars);

            var auth = jObj["auth"];
            if (auth != null) col.Auth = ParsePostmanAuth(auth);

            var events = jObj["event"] as JArray;
            if (events != null)
            {
                ParseEvents(events, out var preReq, out var test);
                col.PreRequestScript = preReq;
                col.TestScript = test;
            }

            return col;
        }
        catch
        {
            return null;
        }
    }

    private static List<CollectionItem> ParsePostmanItems(JArray items)
    {
        var result = new List<CollectionItem>();
        foreach (var item in items)
        {
            var ci = new CollectionItem
            {
                Id = item["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                Name = item["name"]?.ToString() ?? "Unnamed",
                Description = item["description"]?.ToString() ?? ""
            };

            var events = item["event"] as JArray;
            if (events != null)
            {
                ParseEvents(events, out var preReq, out var test);
                ci.PreRequestScript = preReq;
                ci.TestScript = test;
            }

            if (item["item"] is JArray subItems)
            {
                ci.Type = "folder";
                ci.Items = ParsePostmanItems(subItems);
                var auth = item["auth"];
                if (auth != null) ci.Auth = ParsePostmanAuth(auth);
            }
            else if (item["request"] != null)
            {
                ci.Type = "request";
                ci.Request = ParsePostmanRequest(item["request"]!);
            }

            result.Add(ci);
        }
        return result;
    }

    private static ApiRequest ParsePostmanRequest(JToken req)
    {
        var request = new ApiRequest
        {
            Method = req["method"]?.ToString()?.ToUpper() ?? "GET"
        };

        // URL
        var urlToken = req["url"];
        if (urlToken is JObject urlObj)
        {
            request.Url = urlObj["raw"]?.ToString() ?? "";
            var query = urlObj["query"] as JArray;
            if (query != null)
                request.QueryParams = ParsePostmanKeyValues(query);
        }
        else
        {
            request.Url = urlToken?.ToString() ?? "";
        }

        // Headers
        var headers = req["header"] as JArray;
        if (headers != null) request.Headers = ParsePostmanKeyValues(headers);

        // Auth
        var auth = req["auth"];
        if (auth != null) request.Auth = ParsePostmanAuth(auth);

        // Body
        var body = req["body"];
        if (body != null) request.Body = ParsePostmanBody(body);

        // Description as script source
        var desc = req["description"]?.ToString() ?? "";
        if (!string.IsNullOrEmpty(desc)) { /* kept as description */ }

        return request;
    }

    private static List<KeyValueItem> ParsePostmanKeyValues(JArray arr)
    {
        var result = new List<KeyValueItem>();
        foreach (var item in arr)
            result.Add(new KeyValueItem
            {
                Key = item["key"]?.ToString() ?? "",
                Value = item["value"]?.ToString() ?? "",
                Description = item["description"]?.ToString() ?? "",
                Enabled = item["disabled"]?.Value<bool>() != true
            });
        return result;
    }

    private static List<EnvironmentVariable> ParsePostmanVariables(JArray arr)
    {
        var result = new List<EnvironmentVariable>();
        foreach (var item in arr)
            result.Add(new EnvironmentVariable
            {
                Key = item["key"]?.ToString() ?? "",
                Value = item["value"]?.ToString() ?? "",
                InitialValue = item["value"]?.ToString() ?? "",
                Type = item["type"]?.ToString() ?? "text",
                Enabled = item["disabled"]?.Value<bool>() != true
            });
        return result;
    }

    private static AuthConfig ParsePostmanAuth(JToken auth)
    {
        var cfg = new AuthConfig { Type = auth["type"]?.ToString()?.ToLower() ?? "noauth" };
        switch (cfg.Type)
        {
            case "bearer":
                cfg.BearerToken = GetAuthParam(auth, "bearer", "token") ?? "";
                break;
            case "basic":
                cfg.BasicUsername = GetAuthParam(auth, "basic", "username") ?? "";
                cfg.BasicPassword = GetAuthParam(auth, "basic", "password") ?? "";
                break;
            case "apikey":
                cfg.ApiKeyKey = GetAuthParam(auth, "apikey", "key") ?? "X-API-Key";
                cfg.ApiKeyValue = GetAuthParam(auth, "apikey", "value") ?? "";
                cfg.ApiKeyLocation = GetAuthParam(auth, "apikey", "in") ?? "header";
                break;
        }
        return cfg;
    }

    private static string? GetAuthParam(JToken auth, string type, string paramKey)
    {
        var arr = auth[type] as JArray;
        if (arr == null) return null;
        return arr.FirstOrDefault(p => p["key"]?.ToString() == paramKey)?["value"]?.ToString();
    }

    private static RequestBody ParsePostmanBody(JToken body)
    {
        var rb = new RequestBody { Mode = body["mode"]?.ToString() ?? "none" };
        switch (rb.Mode)
        {
            case "raw":
                rb.Raw = body["raw"]?.ToString() ?? "";
                rb.RawLanguage = body["options"]?["raw"]?["language"]?.ToString() ?? "text";
                break;
            case "formdata":
                var fd = body["formdata"] as JArray;
                if (fd != null)
                    foreach (var f in fd)
                        rb.FormData.Add(new FormDataItem
                        {
                            Key = f["key"]?.ToString() ?? "",
                            Value = f["value"]?.ToString() ?? "",
                            Type = f["type"]?.ToString() ?? "text",
                            FilePath = f["src"]?.ToString() ?? "",
                            Enabled = f["disabled"]?.Value<bool>() != true
                        });
                break;
            case "urlencoded":
                var ue = body["urlencoded"] as JArray;
                if (ue != null) rb.UrlEncoded = ParsePostmanKeyValues(ue);
                break;
            case "graphql":
                rb.GraphQL = new GraphQLBody
                {
                    Query = body["graphql"]?["query"]?.ToString() ?? "",
                    Variables = body["graphql"]?["variables"]?.ToString() ?? "{}"
                };
                break;
        }
        return rb;
    }

    private static void ParseEvents(JArray events, out string preReq, out string test)
    {
        preReq = "";
        test = "";
        foreach (var ev in events)
        {
            var listen = ev["listen"]?.ToString();
            var exec = ev["script"]?["exec"];
            string script = "";
            if (exec is JArray arr) script = string.Join("\n", arr.Select(l => l.ToString()));
            else if (exec != null) script = exec.ToString();

            if (listen == "prerequest") preReq = script;
            else if (listen == "test") test = script;
        }
    }

    // ── Export Environment ─────────────────────────────────────────────────

    public string ExportEnvironment(DakiaEnvironment env)
    {
        var obj = new JObject
        {
            ["id"] = env.Id,
            ["name"] = env.Name,
            ["values"] = new JArray(env.Variables.Select(v => new JObject
            {
                ["key"] = v.Key,
                ["value"] = v.Value,
                ["enabled"] = v.Enabled,
                ["type"] = v.Type
            })),
            ["_postman_variable_scope"] = env.IsGlobal ? "globals" : "environment",
            ["_postman_exported_at"] = DateTime.UtcNow.ToString("o"),
            ["_postman_exported_using"] = "Dakia/1.0"
        };
        return obj.ToString(Formatting.Indented);
    }

    public DakiaEnvironment? ImportEnvironment(string json)
    {
        try
        {
            var jObj = JObject.Parse(json);
            var env = new DakiaEnvironment
            {
                Id = jObj["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                Name = jObj["name"]?.ToString() ?? "Imported Environment",
                IsGlobal = jObj["_postman_variable_scope"]?.ToString() == "globals"
            };

            var values = jObj["values"] as JArray;
            if (values != null)
                foreach (var v in values)
                    env.Variables.Add(new EnvironmentVariable
                    {
                        Key = v["key"]?.ToString() ?? "",
                        Value = v["value"]?.ToString() ?? "",
                        InitialValue = v["value"]?.ToString() ?? "",
                        Enabled = v["enabled"]?.Value<bool>() ?? true,
                        Type = v["type"]?.ToString() ?? "text"
                    });

            return env;
        }
        catch { return null; }
    }
}
