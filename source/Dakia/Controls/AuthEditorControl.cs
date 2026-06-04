using Dakia.Models;
using Dakia.Theme;

namespace Dakia.Controls;

public sealed class AuthEditorControl : UserControl
{
    private readonly ComboBox _cmbType;
    private readonly Panel _configPanel;
    public event EventHandler? AuthChanged;

    private AuthConfig _current = new();

    private static readonly string[] AuthTypes =
    [
        "No Auth", "Bearer Token", "Basic Auth", "API Key",
        "OAuth 1.0", "OAuth 2.0", "Digest Auth", "AWS Signature", "JWT Bearer", "NTLM"
    ];

    private static readonly string[] AuthTypeKeys =
    [
        "noauth", "bearer", "basic", "apikey",
        "oauth1", "oauth2", "digest", "awssig", "jwt", "ntlm"
    ];

    public AuthEditorControl()
    {
        _cmbType = new ComboBox();
        _configPanel = new Panel();
        InitComponent();
    }

    private void InitComponent()
    {
        BackColor = AppTheme.Surface;
        var headerPanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = AppTheme.Surface, Padding = new Padding(8, 8, 8, 0) };

        var lbl = new Label { Text = "Auth Type:", AutoSize = true, Location = new Point(8, 11), ForeColor = AppTheme.TextSecondary };
        _cmbType.Location = new Point(80, 7);
        _cmbType.Width = 200;
        _cmbType.FlatStyle = FlatStyle.Flat;
        _cmbType.BackColor = AppTheme.SurfaceElevated;
        _cmbType.ForeColor = AppTheme.TextPrimary;
        _cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbType.Items.AddRange(AuthTypes);
        _cmbType.SelectedIndex = 0;
        _cmbType.SelectedIndexChanged += (_, _) => RenderConfig();

        headerPanel.Controls.Add(lbl);
        headerPanel.Controls.Add(_cmbType);

        _configPanel.Dock = DockStyle.Fill;
        _configPanel.BackColor = AppTheme.Surface;
        _configPanel.AutoScroll = true;

        Controls.Add(_configPanel);
        Controls.Add(headerPanel);
    }

    public void LoadAuth(AuthConfig auth)
    {
        _current = auth;
        var typeIdx = Array.IndexOf(AuthTypeKeys, auth.Type.ToLower());
        _cmbType.SelectedIndex = typeIdx < 0 ? 0 : typeIdx;
        RenderConfig();
    }

    public AuthConfig GetAuth()
    {
        CollectFromPanel();
        return _current;
    }

    private void RenderConfig()
    {
        _current.Type = AuthTypeKeys[Math.Max(0, _cmbType.SelectedIndex)];
        _configPanel.Controls.Clear();

        switch (_current.Type)
        {
            case "noauth":
                AddNote("This request does not use any authorization.");
                break;
            case "bearer":
                AddField("Token", _current.BearerToken, v => _current.BearerToken = v, "Bearer Token", true);
                break;
            case "basic":
                AddField("Username", _current.BasicUsername, v => _current.BasicUsername = v);
                AddField("Password", _current.BasicPassword, v => _current.BasicPassword = v, isPassword: true);
                break;
            case "apikey":
                AddField("Key", _current.ApiKeyKey, v => _current.ApiKeyKey = v);
                AddField("Value", _current.ApiKeyValue, v => _current.ApiKeyValue = v, isPassword: true);
                AddDropdown("Add to", _current.ApiKeyLocation, ["header", "query"], ["Header", "Query Params"], v => _current.ApiKeyLocation = v);
                break;
            case "oauth1":
                AddField("Consumer Key", _current.OAuth1ConsumerKey, v => _current.OAuth1ConsumerKey = v);
                AddField("Consumer Secret", _current.OAuth1ConsumerSecret, v => _current.OAuth1ConsumerSecret = v, isPassword: true);
                AddField("Access Token", _current.OAuth1Token, v => _current.OAuth1Token = v);
                AddField("Token Secret", _current.OAuth1TokenSecret, v => _current.OAuth1TokenSecret = v, isPassword: true);
                AddField("Realm", _current.OAuth1Realm, v => _current.OAuth1Realm = v);
                AddDropdown("Signature Method", _current.OAuth1SignatureMethod,
                    ["HMAC-SHA1", "HMAC-SHA256", "HMAC-SHA512", "RSA-SHA1", "PLAINTEXT"],
                    ["HMAC-SHA1", "HMAC-SHA256", "HMAC-SHA512", "RSA-SHA1", "PLAINTEXT"],
                    v => _current.OAuth1SignatureMethod = v);
                break;
            case "oauth2":
                AddNote("Configure your access token below. For full OAuth 2.0 flow, use 'Get New Access Token'.");
                AddField("Access Token", _current.OAuth2Token, v => _current.OAuth2Token = v, isPassword: true);
                AddField("Header Prefix", _current.OAuth2HeaderPrefix, v => _current.OAuth2HeaderPrefix = v);
                AddDropdown("Add to", _current.OAuth2AddTo, ["header", "query"], ["Request Header", "Query Params"], v => _current.OAuth2AddTo = v);
                AddSeparator("Token Configuration");
                AddField("Token URL", _current.OAuth2TokenUrl, v => _current.OAuth2TokenUrl = v);
                AddField("Client ID", _current.OAuth2ClientId, v => _current.OAuth2ClientId = v);
                AddField("Client Secret", _current.OAuth2ClientSecret, v => _current.OAuth2ClientSecret = v, isPassword: true);
                AddField("Scope", _current.OAuth2Scope, v => _current.OAuth2Scope = v);
                AddDropdown("Grant Type", _current.OAuth2GrantType,
                    ["authorization_code", "client_credentials", "password", "implicit"],
                    ["Authorization Code", "Client Credentials", "Resource Owner Password", "Implicit"],
                    v => _current.OAuth2GrantType = v);
                break;
            case "digest":
                AddField("Username", _current.DigestUsername, v => _current.DigestUsername = v);
                AddField("Password", _current.DigestPassword, v => _current.DigestPassword = v, isPassword: true);
                AddField("Realm", _current.DigestRealm, v => _current.DigestRealm = v);
                AddField("Nonce", _current.DigestNonce, v => _current.DigestNonce = v);
                AddDropdown("Algorithm", _current.DigestAlgorithm,
                    ["MD5", "MD5-sess", "SHA-256", "SHA-256-sess", "SHA-512-256", "SHA-512-256-sess"],
                    ["MD5", "MD5-sess", "SHA-256", "SHA-256-sess", "SHA-512/256", "SHA-512/256-sess"],
                    v => _current.DigestAlgorithm = v);
                break;
            case "awssig":
                AddField("Access Key", _current.AwsAccessKey, v => _current.AwsAccessKey = v);
                AddField("Secret Key", _current.AwsSecretKey, v => _current.AwsSecretKey = v, isPassword: true);
                AddField("AWS Region", _current.AwsRegion, v => _current.AwsRegion = v);
                AddField("Service Name", _current.AwsService, v => _current.AwsService = v);
                AddField("Session Token", _current.AwsSessionToken, v => _current.AwsSessionToken = v, isPassword: true);
                break;
            case "jwt":
                AddDropdown("Algorithm", _current.JwtAlgorithm,
                    ["HS256", "HS384", "HS512", "RS256", "RS384", "RS512", "ES256", "PS256"],
                    ["HS256", "HS384", "HS512", "RS256", "RS384", "RS512", "ES256", "PS256"],
                    v => _current.JwtAlgorithm = v);
                AddField("Secret", _current.JwtSecret, v => _current.JwtSecret = v, isPassword: true);
                AddField("Header Prefix", _current.JwtHeaderPrefix, v => _current.JwtHeaderPrefix = v);
                AddField("Payload", _current.JwtPayload, v => _current.JwtPayload = v, isMultiLine: true);
                break;
            case "ntlm":
                AddField("Username", _current.NtlmUsername, v => _current.NtlmUsername = v);
                AddField("Password", _current.NtlmPassword, v => _current.NtlmPassword = v, isPassword: true);
                AddField("Domain", _current.NtlmDomain, v => _current.NtlmDomain = v);
                AddField("Workstation", _current.NtlmWorkstation, v => _current.NtlmWorkstation = v);
                break;
        }
    }

    private int _yOffset;
    private readonly List<(Action<string> setter, TextBox tb)> _bindings = [];

    private void AddNote(string text)
    {
        _configPanel.Controls.Clear();
        _yOffset = 0;
        _bindings.Clear();
        var lbl = new Label
        {
            Text = text, AutoSize = false, Width = _configPanel.Width - 20,
            Height = 40, Location = new Point(10, 10),
            ForeColor = AppTheme.TextSecondary, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _configPanel.Controls.Add(lbl);
    }

    private void AddField(string label, string value, Action<string> setter,
        string? placeholder = null, bool isPassword = false, bool isMultiLine = false)
    {
        if (_configPanel.Controls.Count == 0 || (_configPanel.Controls.Count == 1 && _configPanel.Controls[0] is Label))
        {
            _configPanel.Controls.Clear();
            _yOffset = 10;
            _bindings.Clear();
        }

        var lbl = new Label
        {
            Text = label + ":", Location = new Point(10, _yOffset + 3),
            Width = 120, ForeColor = AppTheme.TextSecondary
        };
        _configPanel.Controls.Add(lbl);

        var tb = new TextBox
        {
            Location = new Point(140, _yOffset),
            Width = _configPanel.Width - 160,
            BackColor = AppTheme.SurfaceElevated,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Text = value,
            UseSystemPasswordChar = isPassword,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        if (isMultiLine)
        {
            tb.Multiline = true;
            tb.Height = 80;
            tb.ScrollBars = ScrollBars.Vertical;
        }

        tb.TextChanged += (_, _) => { setter(tb.Text); AuthChanged?.Invoke(this, EventArgs.Empty); };
        _configPanel.Controls.Add(tb);
        _bindings.Add((setter, tb));
        _yOffset += isMultiLine ? 90 : 30;
    }

    private void AddDropdown(string label, string value, string[] values, string[] displayValues, Action<string> setter)
    {
        if (_configPanel.Controls.Count == 0)
        {
            _yOffset = 10;
            _bindings.Clear();
        }

        var lbl = new Label
        {
            Text = label + ":", Location = new Point(10, _yOffset + 3),
            Width = 120, ForeColor = AppTheme.TextSecondary
        };
        _configPanel.Controls.Add(lbl);

        var cmb = new ComboBox
        {
            Location = new Point(140, _yOffset),
            Width = 200,
            BackColor = AppTheme.SurfaceElevated,
            ForeColor = AppTheme.TextPrimary,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat
        };
        cmb.Items.AddRange(displayValues);
        var idx = Array.IndexOf(values, value);
        cmb.SelectedIndex = idx < 0 ? 0 : idx;
        cmb.SelectedIndexChanged += (_, _) =>
        {
            setter(values[Math.Max(0, cmb.SelectedIndex)]);
            AuthChanged?.Invoke(this, EventArgs.Empty);
        };
        _configPanel.Controls.Add(cmb);
        _yOffset += 30;
    }

    private void AddSeparator(string title)
    {
        var lbl = new Label
        {
            Text = title, Location = new Point(10, _yOffset + 8),
            AutoSize = true, ForeColor = AppTheme.TextDim, Font = AppTheme.FontSmall
        };
        _configPanel.Controls.Add(lbl);
        _yOffset += 28;
    }

    private void CollectFromPanel() { /* bindings are live via TextChanged */ }
}
