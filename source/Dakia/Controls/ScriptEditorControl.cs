using Dakia.Theme;

namespace Dakia.Controls;

public sealed class ScriptEditorControl : UserControl
{
    private readonly RichTextBox _editor;
    private readonly Panel _toolbar;
    private readonly Label _lblInfo;
    private bool _highlighting;
    public event EventHandler? ScriptChanged;

    public ScriptEditorControl()
    {
        _editor = new RichTextBox();
        _toolbar = new Panel();
        _lblInfo = new Label();
        InitComponent();
    }

    private void InitComponent()
    {
        BackColor = AppTheme.Surface;

        _toolbar.Dock = DockStyle.Top;
        _toolbar.Height = 28;
        _toolbar.BackColor = AppTheme.SurfaceElevated;
        _toolbar.Padding = new Padding(8, 4, 8, 0);

        _lblInfo.Dock = DockStyle.Left;
        _lblInfo.Text = "JavaScript (pm API available)";
        _lblInfo.ForeColor = AppTheme.TextDim;
        _lblInfo.Font = AppTheme.FontSmall;
        _lblInfo.AutoSize = true;

        var btnSnippet = new Button
        {
            Text = "+ Snippet",
            Dock = DockStyle.Right,
            Width = 80, Height = 20,
            BackColor = AppTheme.SurfaceElevated,
            ForeColor = AppTheme.TextSecondary,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSnippet.FlatAppearance.BorderColor = AppTheme.Border;
        btnSnippet.Click += ShowSnippets;

        _toolbar.Controls.Add(_lblInfo);
        _toolbar.Controls.Add(btnSnippet);

        _editor.Dock = DockStyle.Fill;
        _editor.BackColor = AppTheme.SurfaceElevated;
        _editor.ForeColor = AppTheme.TextPrimary;
        _editor.Font = AppTheme.FontMono;
        _editor.BorderStyle = BorderStyle.None;
        _editor.AcceptsTab = true;
        _editor.ScrollBars = RichTextBoxScrollBars.Both;
        _editor.WordWrap = false;
        _editor.TextChanged += (_, _) =>
        {
            ApplySyntaxHighlighting();
            ScriptChanged?.Invoke(this, EventArgs.Empty);
        };
        _editor.KeyDown += HandleKeyDown;

        Controls.Add(_editor);
        Controls.Add(_toolbar);
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string ScriptText
    {
        get => _editor.Text;
        set
        {
            _editor.Text = value;
            ApplySyntaxHighlighting();
        }
    }

    public void SetPlaceholderText(string mode)
    {
        _lblInfo.Text = mode switch
        {
            "prerequest" => "Pre-request Script — runs before the request is sent",
            "test" => "Tests Script — runs after the response is received",
            _ => "JavaScript (pm API available)"
        };
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Tab)
        {
            var sel = _editor.SelectionStart;
            _editor.Text = _editor.Text.Insert(sel, "  ");
            _editor.SelectionStart = sel + 2;
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.OemMinus)
        {
            // Ctrl+/ to comment/uncomment
            ToggleComment();
            e.Handled = true;
        }
    }

    private void ToggleComment()
    {
        var start = _editor.GetFirstCharIndexOfCurrentLine();
        var line = _editor.Lines.Length > 0 ? _editor.Lines[_editor.GetLineFromCharIndex(_editor.SelectionStart)] : "";
        if (line.TrimStart().StartsWith("//"))
        {
            var commentIdx = line.IndexOf("//", StringComparison.Ordinal);
            _editor.Select(start + commentIdx, 2);
            _editor.SelectedText = "";
        }
        else
        {
            _editor.Select(start, 0);
            _editor.SelectedText = "//";
        }
    }

    private void ApplySyntaxHighlighting()
    {
        if (_highlighting || string.IsNullOrEmpty(_editor.Text)) return;
        _highlighting = true;

        var savedPos = _editor.SelectionStart;
        var savedLen = _editor.SelectionLength;
        var text = _editor.Text;

        _editor.SuspendLayout();
        _editor.SelectAll();
        _editor.SelectionColor = AppTheme.TextPrimary;

        HighlightPattern(@"//[^\n]*", AppTheme.SyntaxComment, text);
        HighlightPattern(@"/\*[\s\S]*?\*/", AppTheme.SyntaxComment, text);
        HighlightPattern(@"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'|`(?:[^`\\]|\\.)*`", AppTheme.SyntaxString, text);
        HighlightPattern(@"\b(var|let|const|function|return|if|else|for|while|try|catch|throw|new|this|typeof|instanceof|in|of|class|extends|import|export|default|null|undefined|true|false)\b", AppTheme.SyntaxBool, text);
        HighlightPattern(@"\b(pm|console|require|_)\b", AppTheme.Info, text);
        HighlightPattern(@"\b\d+\.?\d*\b", AppTheme.SyntaxNumber, text);

        _editor.SelectionStart = savedPos;
        _editor.SelectionLength = savedLen;
        _editor.ResumeLayout();
        _highlighting = false;
    }

    private void HighlightPattern(string pattern, Color color, string text)
    {
        try
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                _editor.Select(m.Index, m.Length);
                _editor.SelectionColor = color;
            }
        }
        catch { }
    }

    private void ShowSnippets(object? sender, EventArgs e)
    {
        var menu = new ContextMenuStrip();
        menu.BackColor = AppTheme.Surface;
        menu.ForeColor = AppTheme.TextPrimary;
        menu.Renderer = new Theme.DarkToolStripRenderer();

        void AddSnippet(string label, string code)
        {
            var item = menu.Items.Add(label);
            item.Click += (_, _) =>
            {
                var pos = _editor.SelectionStart;
                _editor.Text = _editor.Text.Insert(pos, code);
                _editor.SelectionStart = pos + code.Length;
                _editor.Focus();
            };
        }

        var preSnippets = menu.Items.Add("Status code checks");
        AddSnippet("  Status is 200", "pm.test(\"Status is 200\", function() {\n    pm.response.to.have.status(200);\n});\n");
        AddSnippet("  Status code is 2xx", "pm.test(\"Status code is 2xx\", function() {\n    pm.response.to.be.ok;\n});\n");
        AddSnippet("  Response time < 200ms", "pm.test(\"Response time is less than 200ms\", function() {\n    pm.expect(pm.response.responseTime).to.be.below(200);\n});\n");
        AddSnippet("  Response has JSON body", "pm.test(\"Response has JSON body\", function() {\n    pm.response.to.have.jsonBody();\n});\n");
        AddSnippet("  Response has header", "pm.test(\"Response has Content-Type\", function() {\n    pm.response.to.have.header(\"Content-Type\");\n});\n");
        menu.Items.Add("-");
        AddSnippet("Set environment variable", "pm.environment.set(\"variable_key\", \"variable_value\");\n");
        AddSnippet("Get environment variable", "var value = pm.environment.get(\"variable_key\");\n");
        AddSnippet("Set collection variable", "pm.collectionVariables.set(\"variable_key\", \"variable_value\");\n");
        AddSnippet("Set global variable", "pm.globals.set(\"variable_key\", \"variable_value\");\n");
        menu.Items.Add("-");
        AddSnippet("Parse JSON body", "var jsonData = pm.response.json();\n");
        AddSnippet("Get body as text", "var body = pm.response.text();\n");
        AddSnippet("Log to console", "console.log(\"value:\", pm.response.json());\n");

        menu.Show(_editor, _editor.PointToClient(Cursor.Position));
    }
}
