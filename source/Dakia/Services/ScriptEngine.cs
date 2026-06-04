using System.Diagnostics;
using Dakia.Models;
using Jint;
using Jint.Runtime;

namespace Dakia.Services;

public sealed class ScriptContext
{
    public ApiRequest Request { get; set; } = new();
    public ApiResponse? Response { get; set; }
    public DakiaEnvironment? Environment { get; set; }
    public DakiaCollection? Collection { get; set; }
    public Dictionary<string, string> Globals { get; set; } = [];
    public Dictionary<string, string> LocalVariables { get; set; } = [];
    public List<TestResult> TestResults { get; } = [];
    public List<string> ConsoleLog { get; } = [];
}

public sealed class ScriptEngine
{
    private static readonly string PmPreamble = """
        function DakiaAssertion(value) {
            this._value = value;
            this._negated = false;
        }
        DakiaAssertion.prototype.__defineGetter__('not', function() {
            var a = new DakiaAssertion(this._value);
            a._negated = !this._negated;
            return a;
        });
        DakiaAssertion.prototype.__defineGetter__('to', function() { return this; });
        DakiaAssertion.prototype.__defineGetter__('be', function() { return this; });
        DakiaAssertion.prototype.__defineGetter__('have', function() { return this; });
        DakiaAssertion.prototype.__defineGetter__('is', function() { return this; });
        DakiaAssertion.prototype.__defineGetter__('and', function() { return this; });
        DakiaAssertion.prototype.equal = function(expected) {
            var eq = this._value == expected;
            if (this._negated ? eq : !eq)
                throw new Error('Expected ' + JSON.stringify(this._value) + (this._negated?' not':'') + ' to equal ' + JSON.stringify(expected));
            return this;
        };
        DakiaAssertion.prototype.eql = DakiaAssertion.prototype.equal;
        DakiaAssertion.prototype.strictEqual = function(expected) {
            var eq = this._value === expected;
            if (this._negated ? eq : !eq)
                throw new Error('Expected ' + JSON.stringify(this._value) + (this._negated?' not':'') + ' to strictly equal ' + JSON.stringify(expected));
            return this;
        };
        DakiaAssertion.prototype.ok = function() {
            var truthy = !!this._value;
            if (this._negated ? truthy : !truthy)
                throw new Error('Expected value to be ' + (this._negated ? 'falsy' : 'truthy'));
            return this;
        };
        DakiaAssertion.prototype.true = function() {
            if (this._negated ? this._value === true : this._value !== true)
                throw new Error('Expected ' + this._value + (this._negated?' not':'') + ' to be true');
            return this;
        };
        DakiaAssertion.prototype.false = function() {
            if (this._negated ? this._value === false : this._value !== false)
                throw new Error('Expected ' + this._value + (this._negated?' not':'') + ' to be false');
            return this;
        };
        DakiaAssertion.prototype.null = function() {
            var isNull = this._value === null || this._value === undefined;
            if (this._negated ? isNull : !isNull)
                throw new Error('Expected value to be null');
            return this;
        };
        DakiaAssertion.prototype.undefined = DakiaAssertion.prototype.null;
        DakiaAssertion.prototype.above = function(n) {
            if (this._negated ? this._value > n : this._value <= n)
                throw new Error('Expected ' + this._value + (this._negated?' not':'') + ' to be above ' + n);
            return this;
        };
        DakiaAssertion.prototype.below = function(n) {
            if (this._negated ? this._value < n : this._value >= n)
                throw new Error('Expected ' + this._value + (this._negated?' not':'') + ' to be below ' + n);
            return this;
        };
        DakiaAssertion.prototype.least = function(n) {
            if (this._negated ? this._value >= n : this._value < n)
                throw new Error('Expected ' + this._value + (this._negated?' not':'') + ' to be at least ' + n);
            return this;
        };
        DakiaAssertion.prototype.most = function(n) {
            if (this._negated ? this._value <= n : this._value > n)
                throw new Error('Expected ' + this._value + (this._negated?' not':'') + ' to be at most ' + n);
            return this;
        };
        DakiaAssertion.prototype.include = function(val) {
            var includes = Array.isArray(this._value) ? this._value.indexOf(val) >= 0
                : typeof this._value === 'string' ? this._value.indexOf(val) >= 0
                : false;
            if (this._negated ? includes : !includes)
                throw new Error('Expected ' + JSON.stringify(this._value) + (this._negated?' not':'') + ' to include ' + JSON.stringify(val));
            return this;
        };
        DakiaAssertion.prototype.contain = DakiaAssertion.prototype.include;
        DakiaAssertion.prototype.an = function(type) {
            var actual = Array.isArray(this._value) ? 'array' : typeof this._value;
            var match = actual === type.toLowerCase();
            if (this._negated ? match : !match)
                throw new Error('Expected type ' + actual + (this._negated?' not':'') + ' to be ' + type);
            return this;
        };
        DakiaAssertion.prototype.a = DakiaAssertion.prototype.an;
        DakiaAssertion.prototype.property = function(name, val) {
            var hasProp = this._value !== null && this._value !== undefined && name in Object(this._value);
            if (this._negated ? hasProp : !hasProp)
                throw new Error('Expected object to' + (this._negated?' not':' ') + 'have property ' + name);
            if (val !== undefined) {
                var eq = this._value[name] == val;
                if (!eq) throw new Error('Expected property ' + name + ' to equal ' + JSON.stringify(val));
            }
            return this;
        };
        DakiaAssertion.prototype.lengthOf = function(n) {
            var len = this._value ? this._value.length : 0;
            if (this._negated ? len === n : len !== n)
                throw new Error('Expected length ' + len + (this._negated?' not':'') + ' to equal ' + n);
            return this;
        };
        DakiaAssertion.prototype.status = function(code) {
            var actual = __responseCode;
            if (this._negated ? actual === code : actual !== code)
                throw new Error('Expected status code ' + actual + (this._negated?' not':'') + ' to be ' + code);
            return this;
        };
        DakiaAssertion.prototype.header = function(name, val) {
            var headerVal = __getResponseHeader(name);
            if (this._negated ? headerVal !== null : headerVal === null)
                throw new Error('Expected response to' + (this._negated?' not':' ') + 'have header ' + name);
            if (val !== undefined && headerVal !== val)
                throw new Error('Expected header ' + name + ' to equal ' + val + ' but got ' + headerVal);
            return this;
        };
        DakiaAssertion.prototype.jsonBody = function() {
            try { JSON.parse(__responseBody); }
            catch(e) { throw new Error('Expected response body to be valid JSON'); }
            return this;
        };
        """;

    public void Execute(string script, ScriptContext ctx)
    {
        if (string.IsNullOrWhiteSpace(script)) return;

        var engine = new Engine(opts =>
        {
            opts.LimitMemory(20 * 1024 * 1024);
            opts.TimeoutInterval(TimeSpan.FromSeconds(30));
            opts.CatchClrExceptions();
        });

        // C# callbacks for pm API
        engine.SetValue("__envGet", (string key) =>
            ctx.Environment?.Variables.FirstOrDefault(v => v.Enabled && v.Key == key)?.Value ?? "");
        engine.SetValue("__envSet", (string key, string val) =>
        {
            if (ctx.Environment == null) return;
            var v = ctx.Environment.Variables.FirstOrDefault(x => x.Key == key);
            if (v != null) v.Value = val; else ctx.Environment.Variables.Add(new EnvironmentVariable { Key = key, Value = val });
        });
        engine.SetValue("__envUnset", (string key) => ctx.Environment?.Variables.RemoveAll(v => v.Key == key));
        engine.SetValue("__envHas", (string key) => ctx.Environment?.Variables.Any(v => v.Key == key) ?? false);

        engine.SetValue("__globGet", (string key) => ctx.Globals.TryGetValue(key, out var v) ? v : "");
        engine.SetValue("__globSet", (string key, string val) => ctx.Globals[key] = val);
        engine.SetValue("__globUnset", (string key) => ctx.Globals.Remove(key));
        engine.SetValue("__globHas", (string key) => ctx.Globals.ContainsKey(key));

        engine.SetValue("__colVarGet", (string key) =>
            ctx.Collection?.Variables.FirstOrDefault(v => v.Enabled && v.Key == key)?.Value ?? "");
        engine.SetValue("__colVarSet", (string key, string val) =>
        {
            if (ctx.Collection == null) return;
            var v = ctx.Collection.Variables.FirstOrDefault(x => x.Key == key);
            if (v != null) v.Value = val; else ctx.Collection.Variables.Add(new EnvironmentVariable { Key = key, Value = val });
        });
        engine.SetValue("__colVarUnset", (string key) => ctx.Collection?.Variables.RemoveAll(v => v.Key == key));
        engine.SetValue("__colVarHas", (string key) => ctx.Collection?.Variables.Any(v => v.Key == key) ?? false);

        engine.SetValue("__localVarGet", (string key) => ctx.LocalVariables.TryGetValue(key, out var v) ? v : "");
        engine.SetValue("__localVarSet", (string key, string val) => ctx.LocalVariables[key] = val);

        engine.SetValue("__addTestResult", (string name, bool passed, string? error) =>
            ctx.TestResults.Add(new TestResult { Name = name, Passed = passed, Error = error }));

        engine.SetValue("__consoleLog", (string msg) => ctx.ConsoleLog.Add(msg));

        engine.SetValue("__responseCode", ctx.Response != null ? (int)ctx.Response.StatusCode : 0);
        engine.SetValue("__responseStatus", ctx.Response?.StatusText ?? "");
        engine.SetValue("__responseTime", ctx.Response?.ResponseTimeMs ?? 0);
        engine.SetValue("__responseSize", ctx.Response?.ResponseSizeBytes ?? 0);
        engine.SetValue("__responseBody", ctx.Response?.Body ?? "");
        engine.SetValue("__responseHeaders", ctx.Response?.Headers ?? []);
        engine.SetValue("__getResponseHeader", (string name) =>
        {
            if (ctx.Response == null) return null;
            var kv = ctx.Response.Headers.FirstOrDefault(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase));
            return kv.Key != null ? kv.Value : null;
        });

        engine.SetValue("__sendRequest", (string jsonReq) =>
        {
            // Stub - full async send not supported in sync script context
            return "{}";
        });

        try
        {
            engine.Execute(PmPreamble);
            engine.Execute(BuildPmApi(ctx));
            engine.Execute(script);
        }
        catch (ExecutionCanceledException)
        {
            ctx.ConsoleLog.Add("[ERROR] Script execution timed out after 30s.");
        }
        catch (Exception ex)
        {
            ctx.ConsoleLog.Add($"[ERROR] {ex.Message}");
        }
    }

    private static string BuildPmApi(ScriptContext ctx)
    {
        var reqHeaders = Newtonsoft.Json.JsonConvert.SerializeObject(
            ctx.Request.Headers.Where(h => h.Enabled).ToDictionary(h => h.Key, h => h.Value));
        var reqUrl = ctx.Request.Url.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var reqMethod = ctx.Request.Method;
        var reqBody = (ctx.Request.Body.Raw ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        return $$"""
        var pm = {
            test: function(name, fn) {
                var start = Date.now();
                try { fn(); __addTestResult(name, true, null); }
                catch(e) { __addTestResult(name, false, e.message || String(e)); }
            },
            expect: function(val) { return new DakiaAssertion(val); },
            environment: {
                get: function(k) { return __envGet(k); },
                set: function(k,v) { __envSet(k, String(v)); },
                unset: function(k) { __envUnset(k); },
                has: function(k) { return __envHas(k); },
                toObject: function() { return {}; }
            },
            globals: {
                get: function(k) { return __globGet(k); },
                set: function(k,v) { __globSet(k, String(v)); },
                unset: function(k) { __globUnset(k); },
                has: function(k) { return __globHas(k); },
                toObject: function() { return {}; }
            },
            collectionVariables: {
                get: function(k) { return __colVarGet(k); },
                set: function(k,v) { __colVarSet(k, String(v)); },
                unset: function(k) { __colVarUnset(k); },
                has: function(k) { return __colVarHas(k); },
                toObject: function() { return {}; }
            },
            variables: {
                get: function(k) { return __localVarGet(k) || __envGet(k) || __colVarGet(k) || __globGet(k) || ''; },
                set: function(k,v) { __localVarSet(k, String(v)); },
                has: function(k) { return !!(__localVarGet(k) || __envGet(k) || __colVarGet(k) || __globGet(k)); },
                toObject: function() { return {}; }
            },
            request: {
                url: { toString: function() { return "{{reqUrl}}"; } },
                method: "{{reqMethod}}",
                headers: { get: function(k) { var h = {{reqHeaders}}; return h[k] || null; } },
                body: { raw: "{{reqBody}}" }
            },
            response: {
                code: __responseCode,
                status: __responseStatus,
                responseTime: __responseTime,
                responseSize: __responseSize,
                text: function() { return __responseBody; },
                json: function() { try { return JSON.parse(__responseBody); } catch(e) { throw new Error('Response body is not valid JSON: ' + e.message); } },
                headers: {
                    get: function(k) { return __getResponseHeader(k); },
                    has: function(k) { return __getResponseHeader(k) !== null; },
                    toObject: function() { return __responseHeaders; }
                },
                to: {
                    have: {
                        status: function(code) {
                            if (__responseCode !== code) throw new Error('Expected status ' + code + ' but got ' + __responseCode);
                        },
                        header: function(name, val) {
                            var h = __getResponseHeader(name);
                            if (h === null) throw new Error('Expected header "' + name + '" to exist');
                            if (val !== undefined && h !== val) throw new Error('Expected header "' + name + '" to be "' + val + '" but got "' + h + '"');
                        },
                        jsonBody: function() {
                            try { JSON.parse(__responseBody); }
                            catch(e) { throw new Error('Expected JSON response body'); }
                        },
                        body: function(expected) {
                            if (__responseBody.indexOf(expected) < 0) throw new Error('Expected body to contain: ' + expected);
                        }
                    },
                    be: {
                        ok: function() {
                            if (__responseCode < 200 || __responseCode >= 300)
                                throw new Error('Expected 2xx status code but got ' + __responseCode);
                        }
                    }
                }
            },
            cookies: { has: function(k) { return false; }, get: function(k) { return null; }, toObject: function() { return {}; } },
            info: { eventName: 'test', iteration: 0, iterationCount: 1, requestName: '', requestId: '' },
            sendRequest: function(req, cb) { try { var r = __sendRequest(JSON.stringify(req)); if(cb) cb(null, JSON.parse(r)); } catch(e) { if(cb) cb(e); } }
        };
        var console = {
            log: function() { __consoleLog(Array.from(arguments).map(function(a) { return typeof a === 'object' ? JSON.stringify(a) : String(a); }).join(' ')); },
            error: function() { __consoleLog('[ERROR] ' + Array.from(arguments).map(function(a) { return typeof a === 'object' ? JSON.stringify(a) : String(a); }).join(' ')); },
            warn: function() { __consoleLog('[WARN] ' + Array.from(arguments).map(function(a) { return typeof a === 'object' ? JSON.stringify(a) : String(a); }).join(' ')); },
            info: function() { __consoleLog('[INFO] ' + Array.from(arguments).map(function(a) { return typeof a === 'object' ? JSON.stringify(a) : String(a); }).join(' ')); }
        };
        var require = function(m) { throw new Error('require() is not supported in Dakia scripts. Module: ' + m); };
        var _ = {
            isEmpty: function(v) { return v == null || (Array.isArray(v) && v.length === 0) || (typeof v === 'string' && v === '') || (typeof v === 'object' && Object.keys(v).length === 0); },
            isArray: Array.isArray,
            isString: function(v) { return typeof v === 'string'; },
            isNumber: function(v) { return typeof v === 'number'; },
            isBoolean: function(v) { return typeof v === 'boolean'; },
            isObject: function(v) { return typeof v === 'object' && v !== null; },
            isNull: function(v) { return v === null; },
            isUndefined: function(v) { return v === undefined; }
        };
        """;
    }
}
