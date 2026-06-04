using System.Text.RegularExpressions;
using Dakia.Models;

namespace Dakia.Services;

public sealed class VariableResolver
{
    private static readonly Regex VarPattern = new(@"\{\{([^{}]+)\}\}", RegexOptions.Compiled);

    public string Resolve(string input,
        DakiaEnvironment? environment = null,
        DakiaCollection? collection = null,
        Dictionary<string, string>? globals = null,
        Dictionary<string, string>? locals = null)
    {
        if (string.IsNullOrEmpty(input) || !input.Contains("{{")) return input;

        return VarPattern.Replace(input, m =>
        {
            var key = m.Groups[1].Value.Trim();
            return LookupVariable(key, environment, collection, globals, locals) ?? m.Value;
        });
    }

    private static string? LookupVariable(
        string key,
        DakiaEnvironment? env,
        DakiaCollection? col,
        Dictionary<string, string>? globals,
        Dictionary<string, string>? locals)
    {
        // Resolution order: local > environment > collection > global > built-in
        if (locals?.TryGetValue(key, out var lv) == true) return lv;

        if (env != null)
        {
            var ev = env.Variables.FirstOrDefault(v => v.Enabled && v.Key == key);
            if (ev != null) return ev.Value;
        }

        if (col != null)
        {
            var cv = col.Variables.FirstOrDefault(v => v.Enabled && v.Key == key);
            if (cv != null) return cv.Value;
        }

        if (globals?.TryGetValue(key, out var gv) == true) return gv;

        return key switch
        {
            "$guid" or "$randomUUID" => Guid.NewGuid().ToString(),
            "$timestamp" => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            "$isoTimestamp" => DateTime.UtcNow.ToString("o"),
            "$randomInt" => Random.Shared.Next(1, 1000).ToString(),
            "$randomFirstName" => RandomFirstName(),
            "$randomLastName" => RandomLastName(),
            "$randomEmail" => $"{RandomFirstName().ToLower()}.{RandomLastName().ToLower()}@example.com",
            "$randomBoolean" => (Random.Shared.Next(2) == 0).ToString().ToLower(),
            "$randomAlphaNumeric" => Guid.NewGuid().ToString("N")[..8],
            _ => null
        };
    }

    public ApiRequest ResolveRequest(
        ApiRequest req,
        DakiaEnvironment? env,
        DakiaCollection? col,
        Dictionary<string, string>? globals = null,
        Dictionary<string, string>? locals = null)
    {
        var resolved = new ApiRequest
        {
            Url = Resolve(req.Url, env, col, globals, locals),
            Method = req.Method,
            PreRequestScript = req.PreRequestScript,
            TestScript = req.TestScript,
            Auth = req.Auth,
            Body = new RequestBody
            {
                Mode = req.Body.Mode,
                Raw = Resolve(req.Body.Raw, env, col, globals, locals),
                RawLanguage = req.Body.RawLanguage,
                BinaryFilePath = req.Body.BinaryFilePath,
                GraphQL = req.Body.GraphQL == null ? null : new GraphQLBody
                {
                    Query = Resolve(req.Body.GraphQL.Query, env, col, globals, locals),
                    Variables = Resolve(req.Body.GraphQL.Variables, env, col, globals, locals)
                }
            }
        };

        foreach (var h in req.Headers)
            resolved.Headers.Add(new KeyValueItem
            {
                Key = Resolve(h.Key, env, col, globals, locals),
                Value = Resolve(h.Value, env, col, globals, locals),
                Enabled = h.Enabled,
                Description = h.Description
            });

        foreach (var p in req.QueryParams)
            resolved.QueryParams.Add(new KeyValueItem
            {
                Key = Resolve(p.Key, env, col, globals, locals),
                Value = Resolve(p.Value, env, col, globals, locals),
                Enabled = p.Enabled,
                Description = p.Description
            });

        foreach (var f in req.Body.FormData)
            resolved.Body.FormData.Add(new FormDataItem
            {
                Key = Resolve(f.Key, env, col, globals, locals),
                Value = Resolve(f.Value, env, col, globals, locals),
                Type = f.Type,
                FilePath = f.FilePath,
                Enabled = f.Enabled,
                Description = f.Description
            });

        foreach (var u in req.Body.UrlEncoded)
            resolved.Body.UrlEncoded.Add(new KeyValueItem
            {
                Key = Resolve(u.Key, env, col, globals, locals),
                Value = Resolve(u.Value, env, col, globals, locals),
                Enabled = u.Enabled,
                Description = u.Description
            });

        return resolved;
    }

    private static string RandomFirstName()
    {
        string[] names = ["James", "Emma", "Liam", "Olivia", "Noah", "Ava", "William", "Sophia", "Benjamin", "Mia"];
        return names[Random.Shared.Next(names.Length)];
    }

    private static string RandomLastName()
    {
        string[] names = ["Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Wilson", "Moore"];
        return names[Random.Shared.Next(names.Length)];
    }
}
