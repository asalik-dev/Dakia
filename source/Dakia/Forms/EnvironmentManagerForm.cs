using Dakia.Controls;
using Dakia.Models;
using Dakia.Services;
using Dakia.Theme;

namespace Dakia.Forms;

public sealed class EnvironmentManagerForm : Form
{
    private readonly ListBox _envList;
    private readonly Panel _editPanel;
    private readonly TextBox _txtName;
    private readonly KeyValueEditorControl _varEditor;
    private readonly Button _btnAdd;
    private readonly Button _btnDuplicate;
    private readonly Button _btnDelete;
    private readonly Button _btnImport;
    private readonly Button _btnExport;
    private readonly CheckBox _chkGlobal;

    private List<DakiaEnvironment> _environments;
    private DakiaEnvironment? _selected;
    private readonly WorkspaceSettings _settings;

    public EnvironmentManagerForm(List<DakiaEnvironment> environments, WorkspaceSettings settings)
    {
        _environments = new List<DakiaEnvironment>(environments);
        _settings = settings;
        _envList = new ListBox();
        _editPanel = new Panel();
        _txtName = new TextBox();
        _varEditor = new KeyValueEditorControl();
        _btnAdd = new Button();
        _btnDuplicate = new Button();
        _btnDelete = new Button();
        _btnImport = new Button();
        _btnExport = new Button();
        _chkGlobal = new CheckBox();
        InitForm();
    }

    private void InitForm()
    {
        Text = "Manage Environments";
        Size = new Size(820, 560);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        MinimizeBox = false;

        // Left panel - env list
        var leftPanel = new Panel { Width = 220, Dock = DockStyle.Left, BackColor = AppTheme.SidebarBg };

        var lstLabel = new Label { Text = "Environments", Dock = DockStyle.Top, Height = 28, BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };

        _envList.Dock = DockStyle.Fill;
        _envList.BackColor = AppTheme.SidebarBg;
        _envList.ForeColor = AppTheme.TextPrimary;
        _envList.BorderStyle = BorderStyle.None;
        _envList.SelectedIndexChanged += OnEnvSelected;

        var listBtnPanel = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = AppTheme.SidebarBg };

        SetupListButton(_btnAdd, "+ Add", 4, 4, AppTheme.Accent, Color.White);
        _btnAdd.Click += (_, _) => AddEnvironment();
        SetupListButton(_btnDuplicate, "Duplicate", 4, 32, AppTheme.SurfaceElevated, AppTheme.TextPrimary);
        _btnDuplicate.Click += (_, _) => DuplicateSelected();
        SetupListButton(_btnImport, "Import", 110, 4, AppTheme.SurfaceElevated, AppTheme.TextPrimary);
        _btnImport.Click += (_, _) => ImportEnvironment();
        SetupListButton(_btnExport, "Export", 110, 32, AppTheme.SurfaceElevated, AppTheme.TextPrimary);
        _btnExport.Click += (_, _) => ExportSelected();
        SetupListButton(_btnDelete, "Delete", 4, 56, AppTheme.SurfaceElevated, AppTheme.Error);
        _btnDelete.FlatAppearance.BorderColor = AppTheme.Error;
        _btnDelete.Click += (_, _) => DeleteSelected();

        listBtnPanel.Controls.Add(_btnAdd);
        listBtnPanel.Controls.Add(_btnDuplicate);
        listBtnPanel.Controls.Add(_btnImport);
        listBtnPanel.Controls.Add(_btnExport);
        listBtnPanel.Controls.Add(_btnDelete);

        leftPanel.Controls.Add(_envList);
        leftPanel.Controls.Add(listBtnPanel);
        leftPanel.Controls.Add(lstLabel);

        // Right panel - editor
        _editPanel.Dock = DockStyle.Fill;
        _editPanel.BackColor = AppTheme.Background;
        _editPanel.Padding = new Padding(12);
        BuildEditPanel();

        // Bottom buttons
        var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = AppTheme.Surface };
        var btnClose = new Button
        {
            Text = "Close",
            Location = new Point(Width - 96, 7),
            Width = 80, Height = 28,
            BackColor = AppTheme.Accent, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (_, _) => Close();
        btnPanel.Controls.Add(btnClose);

        // Separator
        var sep = new Panel { Width = 1, Dock = DockStyle.Left, BackColor = AppTheme.Border, Parent = leftPanel };

        Controls.Add(_editPanel);
        Controls.Add(leftPanel);
        Controls.Add(btnPanel);

        RefreshList();
    }

    private void BuildEditPanel()
    {
        _editPanel.Controls.Clear();

        var namePnl = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = AppTheme.Background };
        var lblName = new Label { Text = "Name:", Location = new Point(0, 6), Width = 60, ForeColor = AppTheme.TextSecondary };
        _txtName.Location = new Point(68, 3);
        _txtName.Width = 300;
        _txtName.BackColor = AppTheme.SurfaceElevated;
        _txtName.ForeColor = AppTheme.TextPrimary;
        _txtName.BorderStyle = BorderStyle.FixedSingle;
        _txtName.TextChanged += (_, _) =>
        {
            if (_selected != null) { _selected.Name = _txtName.Text; SaveSelected(); RefreshList(); }
        };

        _chkGlobal.Text = "Global Variables";
        _chkGlobal.Location = new Point(380, 6);
        _chkGlobal.AutoSize = true;
        _chkGlobal.BackColor = Color.Transparent;
        _chkGlobal.ForeColor = AppTheme.TextSecondary;
        _chkGlobal.CheckedChanged += (_, _) =>
        {
            if (_selected != null) { _selected.IsGlobal = _chkGlobal.Checked; SaveSelected(); }
        };

        namePnl.Controls.Add(lblName);
        namePnl.Controls.Add(_txtName);
        namePnl.Controls.Add(_chkGlobal);

        var varHeader = new Label { Dock = DockStyle.Top, Height = 26, Text = "Variables", ForeColor = AppTheme.TextSecondary, BackColor = AppTheme.SurfaceElevated, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };

        _varEditor.Dock = DockStyle.Fill;
        _varEditor.DataChanged += (_, _) =>
        {
            if (_selected != null)
            {
                _selected.Variables = _varEditor.GetItems().Select(i => new EnvironmentVariable
                {
                    Key = i.Key, Value = i.Value,
                    Enabled = i.Enabled, Description = i.Description
                }).ToList();
                SaveSelected();
            }
        };

        _editPanel.Controls.Add(_varEditor);
        _editPanel.Controls.Add(varHeader);
        _editPanel.Controls.Add(namePnl);
    }

    private void RefreshList()
    {
        var sel = _selected?.Id;
        _envList.Items.Clear();
        foreach (var e in _environments) _envList.Items.Add(e.Name);

        if (sel != null)
        {
            var idx = _environments.FindIndex(e => e.Id == sel);
            if (idx >= 0) _envList.SelectedIndex = idx;
        }
        else if (_envList.Items.Count > 0)
        {
            _envList.SelectedIndex = 0;
        }
    }

    private void OnEnvSelected(object? sender, EventArgs e)
    {
        if (_envList.SelectedIndex < 0 || _envList.SelectedIndex >= _environments.Count) return;
        _selected = _environments[_envList.SelectedIndex];
        _txtName.Text = _selected.Name;
        _chkGlobal.Checked = _selected.IsGlobal;
        _varEditor.LoadItems(_selected.Variables.Select(v => new Models.KeyValueItem
        { Key = v.Key, Value = v.Value, Enabled = v.Enabled, Description = v.Description }).ToList());
    }

    private void AddEnvironment()
    {
        var env = new DakiaEnvironment { Name = "New Environment" };
        StorageService.Instance.SaveEnvironment(env);
        _environments.Add(env);
        RefreshList();
        _envList.SelectedIndex = _environments.Count - 1;
    }

    private void DuplicateSelected()
    {
        if (_selected == null) return;
        var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<DakiaEnvironment>(
            Newtonsoft.Json.JsonConvert.SerializeObject(_selected))!;
        copy.Id = Guid.NewGuid().ToString();
        copy.Name = $"{_selected.Name} (Copy)";
        StorageService.Instance.SaveEnvironment(copy);
        _environments.Add(copy);
        RefreshList();
    }

    private void DeleteSelected()
    {
        if (_selected == null) return;
        if (MessageBox.Show($"Delete environment '{_selected.Name}'?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        StorageService.Instance.DeleteEnvironment(_selected.Id);
        _environments.Remove(_selected);
        _selected = null;
        _txtName.Text = "";
        _varEditor.LoadItems([]);
        RefreshList();
    }

    private void ImportEnvironment()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Import Environment",
            Filter = "JSON Files|*.json|All Files|*.*"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var svc = new ImportExportService();
        var env = svc.ImportEnvironment(File.ReadAllText(dlg.FileName));
        if (env == null) { MessageBox.Show("Invalid environment file.", "Import Error"); return; }

        env.Id = Guid.NewGuid().ToString();
        StorageService.Instance.SaveEnvironment(env);
        _environments.Add(env);
        RefreshList();
        MessageBox.Show($"Environment '{env.Name}' imported.", "Import Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportSelected()
    {
        if (_selected == null) return;
        using var dlg = new SaveFileDialog
        {
            Title = "Export Environment",
            FileName = $"{_selected.Name}.postman_environment.json",
            Filter = "Postman Environment|*.postman_environment.json|JSON|*.json"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var svc = new ImportExportService();
        File.WriteAllText(dlg.FileName, svc.ExportEnvironment(_selected));
        MessageBox.Show($"Exported to {dlg.FileName}", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SaveSelected()
    {
        if (_selected == null) return;
        StorageService.Instance.SaveEnvironment(_selected);
    }

    private static void SetupListButton(Button btn, string text, int x, int y, Color bg, Color fg)
    {
        btn.Text = text;
        btn.Location = new Point(x, y);
        btn.Width = 100;
        btn.Height = 24;
        btn.BackColor = bg;
        btn.ForeColor = fg;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = AppTheme.Border;
        btn.FlatAppearance.BorderSize = 1;
        btn.Cursor = Cursors.Hand;
        btn.Font = AppTheme.FontSmall;
    }
}
