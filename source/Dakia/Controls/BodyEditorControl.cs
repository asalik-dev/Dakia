using Dakia.Models;
using Dakia.Theme;

namespace Dakia.Controls;

public sealed class BodyEditorControl : UserControl
{
    private readonly RadioButton _rbNone, _rbRaw, _rbFormData, _rbUrlEncoded, _rbBinary, _rbGraphQL;
    private readonly Panel _modeBar;
    private readonly Panel _contentPanel;
    private readonly ComboBox _cmbRawLang;

    // Content panels
    private RichTextBox? _rawEditor;
    private DataGridView? _formGrid;
    private DataGridView? _urlGrid;
    private Panel? _binaryPanel;
    private Panel? _graphQLPanel;
    private RichTextBox? _graphQLQuery;
    private RichTextBox? _graphQLVars;

    private string _mode = "none";
    public event EventHandler? BodyChanged;

    public BodyEditorControl()
    {
        _rbNone = new RadioButton();
        _rbRaw = new RadioButton();
        _rbFormData = new RadioButton();
        _rbUrlEncoded = new RadioButton();
        _rbBinary = new RadioButton();
        _rbGraphQL = new RadioButton();
        _modeBar = new Panel();
        _contentPanel = new Panel();
        _cmbRawLang = new ComboBox();
        InitComponent();
    }

    private void InitComponent()
    {
        BackColor = AppTheme.Surface;

        _modeBar.Dock = DockStyle.Top;
        _modeBar.Height = 32;
        _modeBar.BackColor = AppTheme.Surface;
        _modeBar.Padding = new Padding(8, 5, 8, 0);

        SetupRadio(_rbNone, "none", "None", 0);
        SetupRadio(_rbRaw, "raw", "Raw", 55);
        SetupRadio(_rbFormData, "formdata", "Form Data", 105);
        SetupRadio(_rbUrlEncoded, "urlencoded", "x-www-form-urlencoded", 185);
        SetupRadio(_rbBinary, "binary", "Binary", 320);
        SetupRadio(_rbGraphQL, "graphql", "GraphQL", 380);

        _cmbRawLang.Width = 90;
        _cmbRawLang.Location = new Point(445, 4);
        _cmbRawLang.BackColor = AppTheme.SurfaceElevated;
        _cmbRawLang.ForeColor = AppTheme.TextPrimary;
        _cmbRawLang.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbRawLang.FlatStyle = FlatStyle.Flat;
        _cmbRawLang.Items.AddRange(["JSON", "XML", "HTML", "Text", "JavaScript"]);
        _cmbRawLang.SelectedIndex = 0;
        _cmbRawLang.Visible = false;
        _cmbRawLang.SelectedIndexChanged += (_, _) => BodyChanged?.Invoke(this, EventArgs.Empty);
        _modeBar.Controls.Add(_cmbRawLang);

        _rbNone.Checked = true;

        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.BackColor = AppTheme.Surface;

        Controls.Add(_contentPanel);
        Controls.Add(_modeBar);

        RenderContent();
    }

    private void SetupRadio(RadioButton rb, string mode, string text, int x)
    {
        rb.Text = text;
        rb.Location = new Point(x, 5);
        rb.AutoSize = true;
        rb.BackColor = Color.Transparent;
        rb.ForeColor = AppTheme.TextSecondary;
        rb.Cursor = Cursors.Hand;
        rb.CheckedChanged += (_, _) =>
        {
            if (rb.Checked)
            {
                _mode = mode;
                _cmbRawLang.Visible = mode == "raw";
                foreach (Control c in _modeBar.Controls.OfType<RadioButton>())
                    c.ForeColor = AppTheme.TextSecondary;
                rb.ForeColor = AppTheme.TextPrimary;
                RenderContent();
                BodyChanged?.Invoke(this, EventArgs.Empty);
            }
        };
        _modeBar.Controls.Add(rb);
    }

    private void RenderContent()
    {
        _contentPanel.Controls.Clear();

        switch (_mode)
        {
            case "none":
                var lbl = new Label
                {
                    Text = "This request does not have a body.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = AppTheme.TextDim
                };
                _contentPanel.Controls.Add(lbl);
                break;

            case "raw":
                _rawEditor = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    BackColor = AppTheme.SurfaceElevated,
                    ForeColor = AppTheme.TextPrimary,
                    Font = AppTheme.FontMono,
                    BorderStyle = BorderStyle.None,
                    ScrollBars = RichTextBoxScrollBars.Both,
                    WordWrap = false,
                    AcceptsTab = true
                };
                _rawEditor.TextChanged += (_, _) => BodyChanged?.Invoke(this, EventArgs.Empty);
                _contentPanel.Controls.Add(_rawEditor);
                break;

            case "formdata":
                _formGrid = CreateKeyValueGrid(isFormData: true);
                _contentPanel.Controls.Add(_formGrid);
                AddKeyValueToolbar(_contentPanel, _formGrid, true);
                break;

            case "urlencoded":
                _urlGrid = CreateKeyValueGrid();
                _contentPanel.Controls.Add(_urlGrid);
                AddKeyValueToolbar(_contentPanel, _urlGrid, false);
                break;

            case "binary":
                _binaryPanel = CreateBinaryPanel();
                _contentPanel.Controls.Add(_binaryPanel);
                break;

            case "graphql":
                _graphQLPanel = CreateGraphQLPanel();
                _contentPanel.Controls.Add(_graphQLPanel);
                break;
        }
    }

    private DataGridView CreateKeyValueGrid(bool isFormData = false)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            GridColor = AppTheme.Border,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowTemplate = { Height = 26 },
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
            EnableHeadersVisualStyles = false
        };
        grid.DefaultCellStyle.BackColor = AppTheme.Surface;
        grid.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = AppTheme.SelectionBg;
        grid.DefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SurfaceElevated;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.SurfaceElevated;
        grid.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.SurfaceElevated;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = AppTheme.SelectionBg;
        grid.ColumnHeadersHeight = 26;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "", Width = 28, Resizable = DataGridViewTriState.False, TrueValue = true, FalseValue = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Key", HeaderText = "Key", Width = 200, SortMode = DataGridViewColumnSortMode.NotSortable });

        if (isFormData)
        {
            var typeCol = new DataGridViewComboBoxColumn { Name = "Type", HeaderText = "Type", Width = 60, SortMode = DataGridViewColumnSortMode.NotSortable };
            typeCol.Items.AddRange("Text", "File");
            grid.Columns.Add(typeCol);
        }

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", Width = 160, SortMode = DataGridViewColumnSortMode.NotSortable });

        grid.CellValueChanged += (_, _) => BodyChanged?.Invoke(this, EventArgs.Empty);
        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewCheckBoxCell or DataGridViewComboBoxCell)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        return grid;
    }

    private void AddKeyValueToolbar(Panel parent, DataGridView grid, bool isFormData)
    {
        var toolbar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = AppTheme.Surface };
        var btnAdd = new Button
        {
            Text = "+ Add",
            Width = 60, Height = 22, Location = new Point(4, 4),
            BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        btnAdd.FlatAppearance.BorderColor = AppTheme.Border;
        btnAdd.Click += (_, _) =>
        {
            var idx = grid.Rows.Add();
            grid.Rows[idx].Cells["Enabled"].Value = true;
            if (isFormData) grid.Rows[idx].Cells["Type"].Value = "Text";
            BodyChanged?.Invoke(this, EventArgs.Empty);
        };

        var btnDel = new Button
        {
            Text = "Delete",
            Width = 60, Height = 22, Location = new Point(70, 4),
            BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        btnDel.FlatAppearance.BorderColor = AppTheme.Border;
        btnDel.Click += (_, _) =>
        {
            foreach (DataGridViewRow row in grid.SelectedRows) grid.Rows.Remove(row);
            BodyChanged?.Invoke(this, EventArgs.Empty);
        };

        toolbar.Controls.Add(btnAdd);
        toolbar.Controls.Add(btnDel);
        parent.Controls.Add(toolbar);
    }

    private Panel CreateBinaryPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(10) };
        var lbl = new Label { Text = "File:", Location = new Point(10, 15), Width = 40, ForeColor = AppTheme.TextSecondary };
        var tb = new TextBox { Location = new Point(60, 12), Width = 300, BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, ReadOnly = true };
        var btn = new Button
        {
            Text = "Browse",
            Location = new Point(370, 10), Width = 70, Height = 26,
            BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = AppTheme.Border;
        btn.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Title = "Select binary file" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tb.Text = dlg.FileName;
                BodyChanged?.Invoke(this, EventArgs.Empty);
            }
        };
        panel.Controls.Add(lbl);
        panel.Controls.Add(tb);
        panel.Controls.Add(btn);
        return panel;
    }

    private Panel CreateGraphQLPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, BackColor = AppTheme.Border };
        split.SplitterDistance = 200;
        split.SplitterWidth = 3;

        var lblQ = new Label { Text = "Query", Dock = DockStyle.Top, Height = 22, ForeColor = AppTheme.TextSecondary, BackColor = AppTheme.SurfaceElevated, Padding = new Padding(8, 3, 0, 0) };
        _graphQLQuery = new RichTextBox { Dock = DockStyle.Fill, BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextPrimary, Font = AppTheme.FontMono, BorderStyle = BorderStyle.None, AcceptsTab = true };
        _graphQLQuery.TextChanged += (_, _) => BodyChanged?.Invoke(this, EventArgs.Empty);
        split.Panel1.Controls.Add(_graphQLQuery);
        split.Panel1.Controls.Add(lblQ);

        var lblV = new Label { Text = "Variables (JSON)", Dock = DockStyle.Top, Height = 22, ForeColor = AppTheme.TextSecondary, BackColor = AppTheme.SurfaceElevated, Padding = new Padding(8, 3, 0, 0) };
        _graphQLVars = new RichTextBox { Dock = DockStyle.Fill, BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextPrimary, Font = AppTheme.FontMono, BorderStyle = BorderStyle.None, AcceptsTab = true };
        _graphQLVars.TextChanged += (_, _) => BodyChanged?.Invoke(this, EventArgs.Empty);
        split.Panel2.Controls.Add(_graphQLVars);
        split.Panel2.Controls.Add(lblV);

        panel.Controls.Add(split);
        return panel;
    }

    public void LoadBody(RequestBody body)
    {
        _mode = body.Mode;
        switch (body.Mode)
        {
            case "none": _rbNone.Checked = true; break;
            case "raw": _rbRaw.Checked = true; break;
            case "formdata": _rbFormData.Checked = true; break;
            case "urlencoded": _rbUrlEncoded.Checked = true; break;
            case "binary": _rbBinary.Checked = true; break;
            case "graphql": _rbGraphQL.Checked = true; break;
        }
        _cmbRawLang.SelectedIndex = body.RawLanguage switch
        {
            "json" => 0, "xml" => 1, "html" => 2, "text" => 3, "javascript" => 4, _ => 0
        };
        _cmbRawLang.Visible = body.Mode == "raw";

        RenderContent();

        if (body.Mode == "raw" && _rawEditor != null)
            _rawEditor.Text = body.Raw;

        if (body.Mode == "formdata" && _formGrid != null)
        {
            _formGrid.Rows.Clear();
            foreach (var f in body.FormData)
            {
                var idx = _formGrid.Rows.Add();
                _formGrid.Rows[idx].Cells["Enabled"].Value = f.Enabled;
                _formGrid.Rows[idx].Cells["Key"].Value = f.Key;
                _formGrid.Rows[idx].Cells["Type"].Value = f.Type == "file" ? "File" : "Text";
                _formGrid.Rows[idx].Cells["Value"].Value = f.Type == "file" ? f.FilePath : f.Value;
                _formGrid.Rows[idx].Cells["Description"].Value = f.Description;
            }
        }

        if (body.Mode == "urlencoded" && _urlGrid != null)
        {
            _urlGrid.Rows.Clear();
            foreach (var u in body.UrlEncoded)
            {
                var idx = _urlGrid.Rows.Add();
                _urlGrid.Rows[idx].Cells["Enabled"].Value = u.Enabled;
                _urlGrid.Rows[idx].Cells["Key"].Value = u.Key;
                _urlGrid.Rows[idx].Cells["Value"].Value = u.Value;
                _urlGrid.Rows[idx].Cells["Description"].Value = u.Description;
            }
        }

        if (body.Mode == "graphql" && body.GraphQL != null)
        {
            if (_graphQLQuery != null) _graphQLQuery.Text = body.GraphQL.Query;
            if (_graphQLVars != null) _graphQLVars.Text = body.GraphQL.Variables;
        }
    }

    public RequestBody GetBody()
    {
        var body = new RequestBody
        {
            Mode = _mode,
            RawLanguage = _cmbRawLang.SelectedIndex switch { 1 => "xml", 2 => "html", 3 => "text", 4 => "javascript", _ => "json" }
        };

        switch (_mode)
        {
            case "raw":
                body.Raw = _rawEditor?.Text ?? "";
                break;

            case "formdata" when _formGrid != null:
                foreach (DataGridViewRow row in _formGrid.Rows)
                {
                    var key = row.Cells["Key"].Value?.ToString() ?? "";
                    var type = (row.Cells["Type"].Value?.ToString() ?? "Text").ToLower();
                    var value = row.Cells["Value"].Value?.ToString() ?? "";
                    if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(value)) continue;
                    body.FormData.Add(new FormDataItem
                    {
                        Enabled = row.Cells["Enabled"].Value as bool? ?? true,
                        Key = key,
                        Type = type,
                        Value = type == "file" ? "" : value,
                        FilePath = type == "file" ? value : "",
                        Description = row.Cells["Description"].Value?.ToString() ?? ""
                    });
                }
                break;

            case "urlencoded" when _urlGrid != null:
                foreach (DataGridViewRow row in _urlGrid.Rows)
                {
                    var key = row.Cells["Key"].Value?.ToString() ?? "";
                    var value = row.Cells["Value"].Value?.ToString() ?? "";
                    if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(value)) continue;
                    body.UrlEncoded.Add(new KeyValueItem
                    {
                        Enabled = row.Cells["Enabled"].Value as bool? ?? true,
                        Key = key,
                        Value = value,
                        Description = row.Cells["Description"].Value?.ToString() ?? ""
                    });
                }
                break;

            case "binary":
                body.BinaryFilePath = (_binaryPanel?.Controls.OfType<TextBox>().FirstOrDefault()?.Text) ?? "";
                break;

            case "graphql":
                body.GraphQL = new GraphQLBody
                {
                    Query = _graphQLQuery?.Text ?? "",
                    Variables = _graphQLVars?.Text ?? "{}"
                };
                break;
        }
        return body;
    }

    public void SetRawText(string text)
    {
        if (_mode == "raw" && _rawEditor != null)
            _rawEditor.Text = text;
    }

    public string GetRawText() => _rawEditor?.Text ?? "";
}
