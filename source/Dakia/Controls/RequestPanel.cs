using System.Net;
using Dakia.Models;
using Dakia.Services;
using Dakia.Theme;

namespace Dakia.Controls;

public sealed class RequestPanel : UserControl
{
    // URL bar
    private readonly ComboBox _cmbMethod;
    private readonly TextBox _txtUrl;
    private readonly Button _btnSend;
    private readonly Button _btnCancel;
    private readonly Label _lblSaving;

    // Request tabs
    private readonly TabControl _requestTabs;
    private readonly KeyValueEditorControl _paramsEditor;
    private readonly AuthEditorControl _authEditor;
    private readonly KeyValueEditorControl _headersEditor;
    private readonly BodyEditorControl _bodyEditor;
    private readonly ScriptEditorControl _preRequestEditor;
    private readonly ScriptEditorControl _testsEditor;

    // Split + response
    private readonly SplitContainer _split;
    private readonly ResponseViewerControl _responseViewer;

    // State
    private ApiRequest _request = new();
    private DakiaEnvironment? _environment;
    private DakiaCollection? _collection;
    private Dictionary<string, string> _globals = [];
    private CancellationTokenSource? _cts;
    private bool _loading;
    private bool _isDirty;

    public string TabTitle { get; private set; } = "Untitled";

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public CollectionItem? BoundItem { get; set; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public DakiaCollection? BoundCollection { get; set; }

    public event EventHandler<string>? TitleChanged;
    public event EventHandler? RequestSaved;

    private static readonly string[] HttpMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "CONNECT", "TRACE"];
    private readonly WorkspaceSettings _settings;

    public RequestPanel(WorkspaceSettings settings)
    {
        _settings = settings;
        _cmbMethod = new ComboBox();
        _txtUrl = new TextBox();
        _btnSend = new Button();
        _btnCancel = new Button();
        _lblSaving = new Label();
        _requestTabs = new TabControl();
        _paramsEditor = new KeyValueEditorControl();
        _authEditor = new AuthEditorControl();
        _headersEditor = new KeyValueEditorControl(showDescription: false);
        _bodyEditor = new BodyEditorControl();
        _preRequestEditor = new ScriptEditorControl();
        _testsEditor = new ScriptEditorControl();
        _split = new SplitContainer();
        _responseViewer = new ResponseViewerControl();
        InitComponent();
    }

    private void InitComponent()
    {
        BackColor = AppTheme.Background;

        // URL bar
        var urlBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = AppTheme.Background, Padding = new Padding(8, 8, 8, 0) };

        _cmbMethod.Width = 90;
        _cmbMethod.Location = new Point(8, 8);
        _cmbMethod.BackColor = AppTheme.SurfaceElevated;
        _cmbMethod.ForeColor = AppTheme.MethodGet;
        _cmbMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbMethod.FlatStyle = FlatStyle.Flat;
        _cmbMethod.Font = AppTheme.FontBold;
        _cmbMethod.Items.AddRange(HttpMethods);
        _cmbMethod.SelectedIndex = 0;
        _cmbMethod.SelectedIndexChanged += (_, _) =>
        {
            var method = _cmbMethod.SelectedItem?.ToString() ?? "GET";
            _cmbMethod.ForeColor = AppTheme.MethodColor(method);
            MarkDirty();
        };

        _txtUrl.Location = new Point(104, 8);
        _txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _txtUrl.Width = Width - 290;
        _txtUrl.Height = 26;
        _txtUrl.BackColor = AppTheme.SurfaceElevated;
        _txtUrl.ForeColor = AppTheme.TextPrimary;
        _txtUrl.BorderStyle = BorderStyle.FixedSingle;
        _txtUrl.Font = AppTheme.FontMono;
        _txtUrl.PlaceholderText = "Enter URL or paste text";
        _txtUrl.TextChanged += (_, _) => MarkDirty();
        _txtUrl.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) _ = SendRequestAsync(); };

        _btnSend.Text = "Send";
        _btnSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnSend.Width = 70;
        _btnSend.Height = 28;
        _btnSend.BackColor = AppTheme.Accent;
        _btnSend.ForeColor = Color.White;
        _btnSend.FlatStyle = FlatStyle.Flat;
        _btnSend.FlatAppearance.BorderSize = 0;
        _btnSend.Font = AppTheme.FontBold;
        _btnSend.Cursor = Cursors.Hand;
        _btnSend.Click += (_, _) => _ = SendRequestAsync();

        _btnCancel.Text = "Cancel";
        _btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnCancel.Width = 70;
        _btnCancel.Height = 28;
        _btnCancel.BackColor = AppTheme.SurfaceElevated;
        _btnCancel.ForeColor = AppTheme.TextSecondary;
        _btnCancel.FlatStyle = FlatStyle.Flat;
        _btnCancel.FlatAppearance.BorderColor = AppTheme.Border;
        _btnCancel.Cursor = Cursors.Hand;
        _btnCancel.Visible = false;
        _btnCancel.Click += (_, _) => _cts?.Cancel();

        _lblSaving.Text = "Saved";
        _lblSaving.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _lblSaving.AutoSize = true;
        _lblSaving.ForeColor = AppTheme.TextDim;
        _lblSaving.Font = AppTheme.FontSmall;
        _lblSaving.Visible = false;

        urlBar.Controls.Add(_cmbMethod);
        urlBar.Controls.Add(_txtUrl);
        urlBar.Controls.Add(_btnSend);
        urlBar.Controls.Add(_btnCancel);
        urlBar.Controls.Add(_lblSaving);
        urlBar.Resize += (_, _) =>
        {
            _txtUrl.Width = urlBar.Width - 300;
            _btnSend.Location = new Point(urlBar.Width - 84, 9);
            _btnCancel.Location = new Point(urlBar.Width - 160, 9);
            _lblSaving.Location = new Point(urlBar.Width - 230, 14);
        };

        // Request tabs
        _requestTabs.Dock = DockStyle.Fill;
        _requestTabs.BackColor = AppTheme.Background;
        _requestTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _requestTabs.ItemSize = new Size(100, 26);
        _requestTabs.SizeMode = TabSizeMode.Fixed;
        _requestTabs.Padding = new Point(8, 4);
        _requestTabs.DrawItem += DrawRequestTab;

        _preRequestEditor.SetPlaceholderText("prerequest");
        _testsEditor.SetPlaceholderText("test");

        BuildRequestTabs();

        // Split container
        _split.Dock = DockStyle.Fill;
        _split.Orientation = Orientation.Horizontal;
        _split.BackColor = AppTheme.Border;
        _split.SplitterWidth = 4;
        _split.Panel1MinSize = 150;
        _split.Panel2MinSize = 100;

        _split.Panel1.BackColor = AppTheme.Background;
        _split.Panel1.Controls.Add(_requestTabs);

        _split.Panel2.BackColor = AppTheme.Background;
        _split.Panel2.Controls.Add(_responseViewer);

        // Wire data change events
        _paramsEditor.DataChanged += (_, _) => { UpdateUrlFromParams(); MarkDirty(); };
        _authEditor.AuthChanged += (_, _) => MarkDirty();
        _headersEditor.DataChanged += (_, _) => MarkDirty();
        _bodyEditor.BodyChanged += (_, _) => MarkDirty();
        _preRequestEditor.ScriptChanged += (_, _) => MarkDirty();
        _testsEditor.ScriptChanged += (_, _) => MarkDirty();

        Controls.Add(_split);
        Controls.Add(urlBar);

        LoadRequest(new ApiRequest());
    }

    private void BuildRequestTabs()
    {
        TabPage MakeTab(string title, Control content)
        {
            var tp = new TabPage(title) { BackColor = AppTheme.Background, BorderStyle = BorderStyle.None, Padding = new Padding(0) };
            content.Dock = DockStyle.Fill;
            tp.Controls.Add(content);
            return tp;
        }

        _requestTabs.TabPages.Add(MakeTab("Params", _paramsEditor));
        _requestTabs.TabPages.Add(MakeTab("Authorization", _authEditor));
        _requestTabs.TabPages.Add(MakeTab("Headers", _headersEditor));
        _requestTabs.TabPages.Add(MakeTab("Body", _bodyEditor));
        _requestTabs.TabPages.Add(MakeTab("Pre-request Script", _preRequestEditor));
        _requestTabs.TabPages.Add(MakeTab("Tests", _testsEditor));
    }

    public void LoadRequest(ApiRequest req)
    {
        _loading = true;
        _request = req;

        var methodIdx = Array.IndexOf(HttpMethods, req.Method.ToUpper());
        _cmbMethod.SelectedIndex = methodIdx < 0 ? 0 : methodIdx;
        _cmbMethod.ForeColor = AppTheme.MethodColor(req.Method);
        _txtUrl.Text = req.Url;

        _paramsEditor.LoadItems(req.QueryParams);
        _authEditor.LoadAuth(req.Auth);
        _headersEditor.LoadItems(req.Headers);
        _bodyEditor.LoadBody(req.Body);
        _preRequestEditor.ScriptText = req.PreRequestScript;
        _testsEditor.ScriptText = req.TestScript;

        UpdateTabBadges();
        _isDirty = false;
        _loading = false;
    }

    public ApiRequest GetCurrentRequest()
    {
        return new ApiRequest
        {
            Url = _txtUrl.Text,
            Method = _cmbMethod.SelectedItem?.ToString() ?? "GET",
            QueryParams = _paramsEditor.GetItems(),
            Auth = _authEditor.GetAuth(),
            Headers = _headersEditor.GetItems(),
            Body = _bodyEditor.GetBody(),
            PreRequestScript = _preRequestEditor.ScriptText,
            TestScript = _testsEditor.ScriptText
        };
    }

    public void SetEnvironment(DakiaEnvironment? env) => _environment = env;
    public void SetCollection(DakiaCollection? col) => _collection = col;
    public void SetGlobals(Dictionary<string, string> globals) => _globals = globals;

    public void SetSplitDistance(int distance)
    {
        if (distance > 0 && distance < Height - 50)
            _split.SplitterDistance = distance;
    }

    private async Task SendRequestAsync()
    {
        var req = GetCurrentRequest();
        if (string.IsNullOrWhiteSpace(req.Url))
        {
            MessageBox.Show("Please enter a URL.", "No URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Ensure URL has scheme
        if (!req.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !req.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !req.Url.StartsWith("{{"))
            req.Url = $"{_settings.DefaultProtocol}://{req.Url}";

        _btnSend.Enabled = false;
        _btnSend.Text = "...";
        _btnCancel.Visible = true;
        _cts = new CancellationTokenSource();

        try
        {
            // Resolve variables
            var resolver = new VariableResolver();
            var resolvedReq = resolver.ResolveRequest(req, _environment, _collection, _globals);

            // Execute pre-request script
            if (_settings.EnableScripts && !string.IsNullOrWhiteSpace(resolvedReq.PreRequestScript))
            {
                var preCtx = new ScriptContext
                {
                    Request = resolvedReq,
                    Environment = _environment,
                    Collection = _collection,
                    Globals = _globals
                };
                var scriptEngine = new ScriptEngine();
                scriptEngine.Execute(resolvedReq.PreRequestScript, preCtx);

                // Re-resolve after pre-request script may have changed variables
                resolvedReq = resolver.ResolveRequest(req, _environment, _collection, _globals);
            }

            // Send request
            using var httpService = new HttpRequestService(_settings);
            var response = await httpService.SendAsync(resolvedReq, _cts.Token);

            // Execute test/post-request script
            if (_settings.EnableScripts && !string.IsNullOrWhiteSpace(resolvedReq.TestScript))
            {
                var testCtx = new ScriptContext
                {
                    Request = resolvedReq,
                    Response = response,
                    Environment = _environment,
                    Collection = _collection,
                    Globals = _globals
                };
                var scriptEngine = new ScriptEngine();
                scriptEngine.Execute(resolvedReq.TestScript, testCtx);
                response.TestResults.AddRange(testCtx.TestResults);
                response.ConsoleLog.AddRange(testCtx.ConsoleLog);
            }

            // Display response
            Uri? uri = null;
            try { uri = new Uri(resolvedReq.Url); } catch { }
            _responseViewer.ShowResponse(response, uri);

            // Save to history
            var histItem = new RequestHistory
            {
                Request = req,
                RequestName = BoundItem?.Name ?? req.Url,
                Response = response,
                CollectionId = BoundCollection?.Id,
                CollectionName = BoundCollection?.Name
            };
            StorageService.Instance.AddToHistory(histItem, _settings.MaxHistoryItems);

            // Auto-save if bound to collection
            if (_settings.AutoSaveRequests && BoundItem != null && BoundCollection != null && _isDirty)
                SaveToCollection();
        }
        finally
        {
            _btnSend.Enabled = true;
            _btnSend.Text = "Send";
            _btnCancel.Visible = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void UpdateUrlFromParams()
    {
        if (_loading) return;
        var url = _txtUrl.Text;
        var baseUrl = url.Contains('?') ? url[..url.IndexOf('?')] : url;
        var items = _paramsEditor.GetItems().Where(p => p.Enabled && !string.IsNullOrEmpty(p.Key)).ToList();
        if (items.Count > 0)
        {
            var qs = string.Join("&", items.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
            _loading = true;
            _txtUrl.Text = $"{baseUrl}?{qs}";
            _loading = false;
        }
    }

    private void UpdateTabBadges()
    {
        var paramCount = _request.QueryParams.Count(p => p.Enabled && !string.IsNullOrEmpty(p.Key));
        var headerCount = _request.Headers.Count(h => h.Enabled && !string.IsNullOrEmpty(h.Key));
        var authType = _request.Auth.Type;

        _requestTabs.TabPages[0].Text = paramCount > 0 ? $"Params ({paramCount})" : "Params";
        _requestTabs.TabPages[1].Text = authType != "noauth" ? $"Auth ✓" : "Authorization";
        _requestTabs.TabPages[2].Text = headerCount > 0 ? $"Headers ({headerCount})" : "Headers";
        _requestTabs.TabPages[3].Text = _request.Body.Mode != "none" ? $"Body ✓" : "Body";
        _requestTabs.TabPages[4].Text = !string.IsNullOrWhiteSpace(_request.PreRequestScript) ? "Pre-req ✓" : "Pre-request Script";
        _requestTabs.TabPages[5].Text = !string.IsNullOrWhiteSpace(_request.TestScript) ? "Tests ✓" : "Tests";
    }

    private void MarkDirty()
    {
        if (_loading) return;
        _isDirty = true;

        var url = _txtUrl.Text;
        var method = _cmbMethod.SelectedItem?.ToString() ?? "GET";
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                var uri = new Uri(url.Contains("{{") ? "https://example.com/placeholder" : url);
                TabTitle = $"{method} {uri.AbsolutePath.TrimEnd('/')}";
            }
            catch
            {
                TabTitle = $"{method} {url[..Math.Min(30, url.Length)]}";
            }
        }
        else
        {
            TabTitle = "Untitled";
        }

        TitleChanged?.Invoke(this, TabTitle);
    }

    public void SaveToCollection()
    {
        if (BoundItem == null || BoundCollection == null) return;
        BoundItem.Request = GetCurrentRequest();
        BoundItem.UpdatedAt = DateTime.UtcNow;
        StorageService.Instance.SaveCollection(BoundCollection);
        _isDirty = false;
        _lblSaving.Text = "Saved";
        _lblSaving.Visible = true;
        var timer = new System.Windows.Forms.Timer { Interval = 2000 };
        timer.Tick += (_, _) => { _lblSaving.Visible = false; timer.Stop(); timer.Dispose(); };
        timer.Start();
        RequestSaved?.Invoke(this, EventArgs.Empty);
    }

    public bool IsDirty => _isDirty;

    private void DrawRequestTab(object? sender, DrawItemEventArgs e)
    {
        var selected = e.Index == _requestTabs.SelectedIndex;
        e.Graphics.FillRectangle(new SolidBrush(selected ? AppTheme.Background : AppTheme.Surface), e.Bounds);

        if (selected)
        {
            using var accentPen = new Pen(AppTheme.Accent, 2);
            e.Graphics.DrawLine(accentPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        TextRenderer.DrawText(e.Graphics, _requestTabs.TabPages[e.Index].Text,
            AppTheme.FontSmall, e.Bounds,
            selected ? AppTheme.TextPrimary : AppTheme.TextSecondary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
