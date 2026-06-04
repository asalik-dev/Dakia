using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Dakia.Models;
using Dakia.Theme;
using Newtonsoft.Json;

namespace Dakia.Controls;

public sealed class ResponseViewerControl : UserControl
{
    // Status bar
    private readonly Label _lblStatus;
    private readonly Label _lblTime;
    private readonly Label _lblSize;
    private readonly Button _btnCopyBody;
    private readonly Button _btnSaveBody;
    private readonly Panel _statusBar;

    // Response tabs
    private readonly TabControl _tabs;

    // Body tab
    private readonly Panel _bodyTab;
    private readonly RadioButton _rbPretty, _rbRaw, _rbPreview;
    private readonly RichTextBox _bodyViewer;
    private readonly WebBrowser? _webPreview;

    // Headers tab
    private readonly DataGridView _headersGrid;

    // Cookies tab
    private readonly DataGridView _cookiesGrid;

    // Tests tab
    private readonly Panel _testsPanel;

    // Console tab
    private readonly ListBox _consoleList;

    private ApiResponse? _currentResponse;
    private bool _highlighting;

    public ResponseViewerControl()
    {
        _lblStatus = new Label();
        _lblTime = new Label();
        _lblSize = new Label();
        _btnCopyBody = new Button();
        _btnSaveBody = new Button();
        _statusBar = new Panel();
        _tabs = new TabControl();
        _bodyTab = new Panel();
        _rbPretty = new RadioButton();
        _rbRaw = new RadioButton();
        _rbPreview = new RadioButton();
        _bodyViewer = new RichTextBox();
        _headersGrid = new DataGridView();
        _cookiesGrid = new DataGridView();
        _testsPanel = new Panel();
        _consoleList = new ListBox();

        try { _webPreview = new WebBrowser(); } catch { _webPreview = null; }

        InitComponent();
    }

    private void InitComponent()
    {
        BackColor = AppTheme.Surface;

        // Status bar
        _statusBar.Dock = DockStyle.Top;
        _statusBar.Height = 32;
        _statusBar.BackColor = AppTheme.SurfaceElevated;
        _statusBar.Padding = new Padding(8, 0, 8, 0);

        _lblStatus.AutoSize = true;
        _lblStatus.Location = new Point(8, 7);
        _lblStatus.ForeColor = AppTheme.TextDim;
        _lblStatus.Text = "No response yet";

        _lblTime.AutoSize = true;
        _lblTime.Location = new Point(160, 7);
        _lblTime.ForeColor = AppTheme.TextDim;

        _lblSize.AutoSize = true;
        _lblSize.Location = new Point(280, 7);
        _lblSize.ForeColor = AppTheme.TextDim;

        _btnCopyBody.Text = "Copy";
        _btnCopyBody.Width = 55;
        _btnCopyBody.Height = 22;
        _btnCopyBody.BackColor = AppTheme.SurfaceElevated;
        _btnCopyBody.ForeColor = AppTheme.TextSecondary;
        _btnCopyBody.FlatStyle = FlatStyle.Flat;
        _btnCopyBody.FlatAppearance.BorderColor = AppTheme.Border;
        _btnCopyBody.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _btnCopyBody.Location = new Point(900, 5);
        _btnCopyBody.Cursor = Cursors.Hand;
        _btnCopyBody.Click += (_, _) =>
        {
            if (_currentResponse != null) Clipboard.SetText(_currentResponse.Body);
        };

        _btnSaveBody.Text = "Save";
        _btnSaveBody.Width = 55;
        _btnSaveBody.Height = 22;
        _btnSaveBody.BackColor = AppTheme.SurfaceElevated;
        _btnSaveBody.ForeColor = AppTheme.TextSecondary;
        _btnSaveBody.FlatStyle = FlatStyle.Flat;
        _btnSaveBody.FlatAppearance.BorderColor = AppTheme.Border;
        _btnSaveBody.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _btnSaveBody.Location = new Point(960, 5);
        _btnSaveBody.Cursor = Cursors.Hand;
        _btnSaveBody.Click += SaveBody;

        _statusBar.Controls.Add(_lblStatus);
        _statusBar.Controls.Add(_lblTime);
        _statusBar.Controls.Add(_lblSize);
        _statusBar.Controls.Add(_btnCopyBody);
        _statusBar.Controls.Add(_btnSaveBody);

        // Tabs
        _tabs.Dock = DockStyle.Fill;
        _tabs.BackColor = AppTheme.Surface;
        _tabs.Padding = new Point(12, 4);
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.ItemSize = new Size(90, 26);
        _tabs.DrawItem += DrawTabItem;

        // Body tab
        var tpBody = new TabPage("Body") { BackColor = AppTheme.Surface, BorderStyle = BorderStyle.None };
        BuildBodyTab(tpBody);

        // Headers tab
        var tpHeaders = new TabPage("Headers") { BackColor = AppTheme.Surface };
        BuildHeadersTab(tpHeaders);

        // Cookies tab
        var tpCookies = new TabPage("Cookies") { BackColor = AppTheme.Surface };
        BuildCookiesTab(tpCookies);

        // Tests tab
        var tpTests = new TabPage("Test Results") { BackColor = AppTheme.Surface };
        BuildTestsTab(tpTests);

        // Console tab
        var tpConsole = new TabPage("Console") { BackColor = AppTheme.Surface };
        BuildConsoleTab(tpConsole);

        _tabs.TabPages.Add(tpBody);
        _tabs.TabPages.Add(tpHeaders);
        _tabs.TabPages.Add(tpCookies);
        _tabs.TabPages.Add(tpTests);
        _tabs.TabPages.Add(tpConsole);

        Controls.Add(_tabs);
        Controls.Add(_statusBar);
    }

    private void BuildBodyTab(TabPage parent)
    {
        var viewBar = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = AppTheme.Surface };

        SetupViewRadio(_rbPretty, "Pretty", 8, viewBar);
        SetupViewRadio(_rbRaw, "Raw", 68, viewBar);
        SetupViewRadio(_rbPreview, "Preview", 110, viewBar);
        _rbPretty.Checked = true;

        _bodyViewer.Dock = DockStyle.Fill;
        _bodyViewer.BackColor = AppTheme.SurfaceElevated;
        _bodyViewer.ForeColor = AppTheme.TextPrimary;
        _bodyViewer.Font = AppTheme.FontMono;
        _bodyViewer.BorderStyle = BorderStyle.None;
        _bodyViewer.ReadOnly = true;
        _bodyViewer.ScrollBars = RichTextBoxScrollBars.Both;
        _bodyViewer.WordWrap = false;

        if (_webPreview != null)
        {
            _webPreview.Dock = DockStyle.Fill;
            _webPreview.Visible = false;
            _webPreview.ScriptErrorsSuppressed = true;
            parent.Controls.Add(_webPreview);
        }

        parent.Controls.Add(_bodyViewer);
        parent.Controls.Add(viewBar);
    }

    private void SetupViewRadio(RadioButton rb, string text, int x, Panel parent)
    {
        rb.Text = text;
        rb.Location = new Point(x, 4);
        rb.AutoSize = true;
        rb.BackColor = Color.Transparent;
        rb.ForeColor = AppTheme.TextSecondary;
        rb.Cursor = Cursors.Hand;
        rb.CheckedChanged += (_, _) =>
        {
            if (!rb.Checked) return;
            foreach (var r in new[] { _rbPretty, _rbRaw, _rbPreview })
                r.ForeColor = r.Checked ? AppTheme.TextPrimary : AppTheme.TextSecondary;
            UpdateBodyView();
        };
        parent.Controls.Add(rb);
    }

    private void BuildHeadersTab(TabPage parent)
    {
        _headersGrid.Dock = DockStyle.Fill;
        _headersGrid.BackgroundColor = AppTheme.Surface;
        _headersGrid.ForeColor = AppTheme.TextPrimary;
        _headersGrid.GridColor = AppTheme.Border;
        _headersGrid.BorderStyle = BorderStyle.None;
        _headersGrid.RowHeadersVisible = false;
        _headersGrid.ReadOnly = true;
        _headersGrid.AllowUserToAddRows = false;
        _headersGrid.AllowUserToResizeRows = false;
        _headersGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _headersGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _headersGrid.RowTemplate.Height = 24;
        _headersGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _headersGrid.EnableHeadersVisualStyles = false;
        _headersGrid.DefaultCellStyle.BackColor = AppTheme.Surface;
        _headersGrid.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        _headersGrid.DefaultCellStyle.SelectionBackColor = AppTheme.SelectionBg;
        _headersGrid.DefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;
        _headersGrid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SurfaceElevated;
        _headersGrid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextSecondary;
        _headersGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.SurfaceElevated;
        _headersGrid.ColumnHeadersHeight = 26;
        _headersGrid.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.SurfaceElevated;
        _headersGrid.AlternatingRowsDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        _headersGrid.AlternatingRowsDefaultCellStyle.SelectionBackColor = AppTheme.SelectionBg;

        _headersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Header", Width = 250, SortMode = DataGridViewColumnSortMode.NotSortable });
        _headersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable });

        parent.Controls.Add(_headersGrid);
    }

    private void BuildCookiesTab(TabPage parent)
    {
        _cookiesGrid.Dock = DockStyle.Fill;
        _cookiesGrid.BackgroundColor = AppTheme.Surface;
        _cookiesGrid.ForeColor = AppTheme.TextPrimary;
        _cookiesGrid.GridColor = AppTheme.Border;
        _cookiesGrid.BorderStyle = BorderStyle.None;
        _cookiesGrid.RowHeadersVisible = false;
        _cookiesGrid.ReadOnly = true;
        _cookiesGrid.AllowUserToAddRows = false;
        _cookiesGrid.AllowUserToResizeRows = false;
        _cookiesGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _cookiesGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _cookiesGrid.RowTemplate.Height = 24;
        _cookiesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _cookiesGrid.EnableHeadersVisualStyles = false;
        _cookiesGrid.DefaultCellStyle.BackColor = AppTheme.Surface;
        _cookiesGrid.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        _cookiesGrid.DefaultCellStyle.SelectionBackColor = AppTheme.SelectionBg;
        _cookiesGrid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SurfaceElevated;
        _cookiesGrid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextSecondary;
        _cookiesGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.SurfaceElevated;
        _cookiesGrid.ColumnHeadersHeight = 26;
        _cookiesGrid.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.SurfaceElevated;
        _cookiesGrid.AlternatingRowsDefaultCellStyle.ForeColor = AppTheme.TextPrimary;

        _cookiesGrid.Columns.Add("Name", "Name");
        _cookiesGrid.Columns.Add("Value", "Value");
        _cookiesGrid.Columns.Add("Domain", "Domain");
        _cookiesGrid.Columns.Add("Path", "Path");
        _cookiesGrid.Columns.Add("Expires", "Expires");
        _cookiesGrid.Columns.Add("Secure", "Secure");
        _cookiesGrid.Columns.Add("HttpOnly", "HttpOnly");
        _cookiesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

        parent.Controls.Add(_cookiesGrid);
    }

    private void BuildTestsTab(TabPage parent)
    {
        _testsPanel.Dock = DockStyle.Fill;
        _testsPanel.BackColor = AppTheme.Surface;
        _testsPanel.AutoScroll = true;
        parent.Controls.Add(_testsPanel);
    }

    private void BuildConsoleTab(TabPage parent)
    {
        _consoleList.Dock = DockStyle.Fill;
        _consoleList.BackColor = AppTheme.SurfaceElevated;
        _consoleList.ForeColor = AppTheme.TextPrimary;
        _consoleList.Font = AppTheme.FontMonoSmall;
        _consoleList.BorderStyle = BorderStyle.None;
        _consoleList.IntegralHeight = false;
        parent.Controls.Add(_consoleList);
    }

    public void ShowResponse(ApiResponse response, Uri? requestUri = null)
    {
        _currentResponse = response;

        if (response.ErrorMessage != null)
        {
            _lblStatus.Text = "Error";
            _lblStatus.ForeColor = AppTheme.Error;
            _lblTime.Text = $"{response.ResponseTimeMs}ms";
            _lblSize.Text = "";
            _bodyViewer.ForeColor = AppTheme.Error;
            _bodyViewer.Text = response.ErrorMessage;
            return;
        }

        var code = (int)response.StatusCode;
        _lblStatus.Text = $"{code} {response.StatusText}";
        _lblStatus.ForeColor = AppTheme.StatusColor(code);
        _lblTime.Text = $"{response.ResponseTimeMs}ms";
        _lblSize.Text = FormatBytes(response.ResponseSizeBytes);
        _lblTime.ForeColor = response.ResponseTimeMs < 500 ? AppTheme.Success : response.ResponseTimeMs < 2000 ? AppTheme.Warning : AppTheme.Error;

        UpdateBodyView();
        UpdateHeaders(response);
        UpdateCookies(response, requestUri);
        UpdateTests(response);
        UpdateConsole(response);
    }

    private void UpdateBodyView()
    {
        if (_currentResponse == null) return;

        if (_rbPreview.Checked && _webPreview != null)
        {
            _bodyViewer.Visible = false;
            _webPreview.Visible = true;
            if (_currentResponse.ContentType.Contains("html", StringComparison.OrdinalIgnoreCase))
                _webPreview.DocumentText = _currentResponse.Body;
            else
                _webPreview.DocumentText = $"<pre>{System.Net.WebUtility.HtmlEncode(_currentResponse.Body)}</pre>";
            return;
        }

        if (_webPreview != null) _webPreview.Visible = false;
        _bodyViewer.Visible = true;

        if (_rbRaw.Checked)
        {
            _bodyViewer.ForeColor = AppTheme.TextPrimary;
            _bodyViewer.Text = _currentResponse.Body;
            return;
        }

        // Pretty mode
        var body = _currentResponse.Body;
        var ct = _currentResponse.ContentType;

        if (ct.Contains("json", StringComparison.OrdinalIgnoreCase) || IsJsonBody(body))
        {
            var pretty = PrettifyJson(body);
            _bodyViewer.Text = pretty;
            ApplyJsonHighlighting(pretty);
        }
        else if (ct.Contains("xml", StringComparison.OrdinalIgnoreCase))
        {
            _bodyViewer.Text = PrettifyXml(body);
            _bodyViewer.ForeColor = AppTheme.TextPrimary;
        }
        else
        {
            _bodyViewer.ForeColor = AppTheme.TextPrimary;
            _bodyViewer.Text = body;
        }
    }

    private void ApplyJsonHighlighting(string text)
    {
        if (_highlighting) return;
        _highlighting = true;

        var savedPos = _bodyViewer.SelectionStart;
        _bodyViewer.SuspendLayout();
        _bodyViewer.SelectAll();
        _bodyViewer.SelectionColor = AppTheme.TextPrimary;

        void Colorize(string pattern, Color color)
        {
            foreach (Match m in Regex.Matches(text, pattern))
            {
                _bodyViewer.Select(m.Index, m.Length);
                _bodyViewer.SelectionColor = color;
            }
        }

        // Keys (before colon)
        Colorize(@"""[^""\\]*(?:\\.[^""\\]*)*""\s*:", AppTheme.SyntaxKey);
        // String values
        Colorize(@":\s*(""[^""\\]*(?:\\.[^""\\]*)*"")", AppTheme.SyntaxString);
        // Numbers
        Colorize(@":\s*(-?\d+\.?\d*(?:[eE][+-]?\d+)?)", AppTheme.SyntaxNumber);
        // Booleans
        Colorize(@"\b(true|false)\b", AppTheme.SyntaxBool);
        // Null
        Colorize(@"\bnull\b", AppTheme.SyntaxNull);
        // Braces/brackets
        Colorize(@"[{}\[\]]", AppTheme.SyntaxBrace);

        _bodyViewer.SelectionStart = savedPos;
        _bodyViewer.SelectionLength = 0;
        _bodyViewer.ResumeLayout();
        _highlighting = false;
    }

    private void UpdateHeaders(ApiResponse response)
    {
        _headersGrid.Rows.Clear();
        foreach (var h in response.Headers.OrderBy(h => h.Key))
        {
            var idx = _headersGrid.Rows.Add();
            _headersGrid.Rows[idx].Cells["Name"].Value = h.Key;
            _headersGrid.Rows[idx].Cells["Value"].Value = h.Value;
        }

        // Update tab title
        var tp = _tabs.TabPages[1];
        tp.Text = $"Headers ({response.Headers.Count})";
    }

    private void UpdateCookies(ApiResponse response, Uri? requestUri)
    {
        _cookiesGrid.Rows.Clear();
        if (response.Headers.TryGetValue("Set-Cookie", out var cookieStr))
        {
            var parts = cookieStr.Split(';');
            if (parts.Length > 0)
            {
                var kv = parts[0].Split('=', 2);
                var idx = _cookiesGrid.Rows.Add();
                if (kv.Length == 2)
                {
                    _cookiesGrid.Rows[idx].Cells["Name"].Value = kv[0].Trim();
                    _cookiesGrid.Rows[idx].Cells["Value"].Value = kv[1].Trim();
                }
                _cookiesGrid.Rows[idx].Cells["Domain"].Value = requestUri?.Host ?? "";
                _cookiesGrid.Rows[idx].Cells["Path"].Value = "/";
                foreach (var part in parts.Skip(1))
                {
                    var p = part.Trim();
                    if (p.StartsWith("expires=", StringComparison.OrdinalIgnoreCase))
                        _cookiesGrid.Rows[idx].Cells["Expires"].Value = p[8..];
                    else if (p.Equals("secure", StringComparison.OrdinalIgnoreCase))
                        _cookiesGrid.Rows[idx].Cells["Secure"].Value = "Yes";
                    else if (p.Equals("httponly", StringComparison.OrdinalIgnoreCase))
                        _cookiesGrid.Rows[idx].Cells["HttpOnly"].Value = "Yes";
                }
            }
        }
    }

    private void UpdateTests(ApiResponse response)
    {
        _testsPanel.Controls.Clear();
        if (response.TestResults.Count == 0)
        {
            var lbl = new Label
            {
                Text = "No tests were run.\nAdd test scripts in the Tests tab of the request.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = AppTheme.TextDim
            };
            _testsPanel.Controls.Add(lbl);
            _tabs.TabPages[3].Text = "Test Results";
            return;
        }

        int pass = response.TestResults.Count(t => t.Passed);
        int fail = response.TestResults.Count(t => !t.Passed);
        _tabs.TabPages[3].Text = $"Tests ({pass}/{response.TestResults.Count})";

        int y = 8;
        var summaryLbl = new Label
        {
            Text = $"{pass} passing, {fail} failing",
            Location = new Point(10, y),
            AutoSize = true,
            ForeColor = fail > 0 ? AppTheme.Error : AppTheme.Success,
            Font = AppTheme.FontBold
        };
        _testsPanel.Controls.Add(summaryLbl);
        y += 30;

        foreach (var test in response.TestResults)
        {
            var icon = test.Passed ? "✓" : "✗";
            var color = test.Passed ? AppTheme.Success : AppTheme.Error;
            var testLbl = new Label
            {
                Text = $"{icon}  {test.Name}",
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = color
            };
            _testsPanel.Controls.Add(testLbl);
            y += 22;

            if (!test.Passed && test.Error != null)
            {
                var errLbl = new Label
                {
                    Text = $"    {test.Error}",
                    Location = new Point(10, y),
                    AutoSize = true,
                    ForeColor = AppTheme.Error,
                    Font = AppTheme.FontSmall
                };
                _testsPanel.Controls.Add(errLbl);
                y += 18;
            }
        }
    }

    private void UpdateConsole(ApiResponse response)
    {
        _consoleList.Items.Clear();
        foreach (var log in response.ConsoleLog)
            _consoleList.Items.Add(log);
        if (response.ConsoleLog.Count > 0)
            _tabs.TabPages[4].Text = $"Console ({response.ConsoleLog.Count})";
        else
            _tabs.TabPages[4].Text = "Console";
    }

    public void ShowEmpty()
    {
        _currentResponse = null;
        _lblStatus.Text = "No response yet";
        _lblStatus.ForeColor = AppTheme.TextDim;
        _lblTime.Text = "";
        _lblSize.Text = "";
        _bodyViewer.Text = "";
        _headersGrid.Rows.Clear();
        _testsPanel.Controls.Clear();
        _consoleList.Items.Clear();
        for (int i = 0; i < _tabs.TabPages.Count; i++)
            _tabs.TabPages[i].Text = i switch { 1 => "Headers", 2 => "Cookies", 3 => "Test Results", 4 => "Console", _ => "Body" };
    }

    private static string PrettifyJson(string json)
    {
        try
        {
            var obj = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
        catch { return json; }
    }

    private static string PrettifyXml(string xml)
    {
        try
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml);
            var sb = new StringBuilder();
            using var writer = new System.Xml.XmlTextWriter(new StringWriter(sb)) { Formatting = System.Xml.Formatting.Indented };
            doc.WriteTo(writer);
            return sb.ToString();
        }
        catch { return xml; }
    }

    private static bool IsJsonBody(string body)
    {
        var t = body.TrimStart();
        return t.StartsWith('{') || t.StartsWith('[');
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024):F1} MB";
    }

    private void SaveBody(object? sender, EventArgs e)
    {
        if (_currentResponse == null) return;
        using var dlg = new SaveFileDialog
        {
            Title = "Save Response Body",
            Filter = "JSON Files|*.json|XML Files|*.xml|Text Files|*.txt|All Files|*.*"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            File.WriteAllText(dlg.FileName, _currentResponse.Body, Encoding.UTF8);
    }

    private void DrawTabItem(object? sender, DrawItemEventArgs e)
    {
        e.Graphics.FillRectangle(
            new SolidBrush(e.Index == _tabs.SelectedIndex ? AppTheme.TabActiveBg : AppTheme.Surface),
            e.Bounds);
        TextRenderer.DrawText(e.Graphics, _tabs.TabPages[e.Index].Text,
            AppTheme.FontNormal, e.Bounds,
            e.Index == _tabs.SelectedIndex ? AppTheme.TextPrimary : AppTheme.TextSecondary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
