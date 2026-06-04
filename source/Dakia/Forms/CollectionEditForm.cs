using Dakia.Controls;
using Dakia.Models;
using Dakia.Theme;

namespace Dakia.Forms;

public sealed class CollectionEditForm : Form
{
    private readonly TextBox _txtName;
    private readonly TextBox _txtDescription;
    private readonly AuthEditorControl _authEditor;
    private readonly ScriptEditorControl _preReqEditor;
    private readonly ScriptEditorControl _testEditor;
    private readonly KeyValueEditorControl _varEditor;

    public DakiaCollection ResultCollection { get; private set; }

    public CollectionEditForm(DakiaCollection? existing)
    {
        ResultCollection = existing ?? new DakiaCollection();
        _txtName = new TextBox();
        _txtDescription = new TextBox();
        _authEditor = new AuthEditorControl();
        _preReqEditor = new ScriptEditorControl();
        _testEditor = new ScriptEditorControl();
        _varEditor = new KeyValueEditorControl();
        InitForm();
    }

    private void InitForm()
    {
        Text = ResultCollection.Id == "" || ResultCollection.Items.Count == 0 && ResultCollection.Name == "New Collection"
            ? "New Collection" : "Edit Collection";
        Size = new Size(640, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        MinimizeBox = false;

        var tabs = new TabControl { Dock = DockStyle.Fill, DrawMode = TabDrawMode.OwnerDrawFixed, ItemSize = new Size(100, 26) };
        tabs.DrawItem += (_, e) =>
        {
            e.Graphics.FillRectangle(new SolidBrush(e.Index == tabs.SelectedIndex ? AppTheme.Background : AppTheme.Surface), e.Bounds);
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, AppTheme.FontNormal, e.Bounds,
                e.Index == tabs.SelectedIndex ? AppTheme.TextPrimary : AppTheme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        // Info tab
        var infoTab = new TabPage("Info") { BackColor = AppTheme.Background };
        AddLabeledControl(infoTab, "Name:", _txtName, 12, 12);
        _txtName.Text = ResultCollection.Name;
        _txtName.Width = 450;
        AddLabeledControl(infoTab, "Description:", _txtDescription, 12, 42);
        _txtDescription.Text = ResultCollection.Description;
        _txtDescription.Width = 450;
        _txtDescription.Multiline = true;
        _txtDescription.Height = 60;

        // Auth tab
        var authTab = new TabPage("Authorization") { BackColor = AppTheme.Background };
        _authEditor.Dock = DockStyle.Fill;
        authTab.Controls.Add(_authEditor);
        _authEditor.LoadAuth(ResultCollection.Auth);

        // Variables tab
        var varsTab = new TabPage("Variables") { BackColor = AppTheme.Background };
        _varEditor.Dock = DockStyle.Fill;
        varsTab.Controls.Add(_varEditor);
        _varEditor.LoadItems(ResultCollection.Variables.Select(v => new Models.KeyValueItem
        { Key = v.Key, Value = v.Value, Enabled = v.Enabled, Description = v.Description }).ToList());

        // Pre-req tab
        var preReqTab = new TabPage("Pre-request Script") { BackColor = AppTheme.Background };
        _preReqEditor.Dock = DockStyle.Fill;
        preReqTab.Controls.Add(_preReqEditor);
        _preReqEditor.ScriptText = ResultCollection.PreRequestScript;

        // Tests tab
        var testTab = new TabPage("Tests") { BackColor = AppTheme.Background };
        _testEditor.Dock = DockStyle.Fill;
        testTab.Controls.Add(_testEditor);
        _testEditor.ScriptText = ResultCollection.TestScript;

        tabs.TabPages.Add(infoTab);
        tabs.TabPages.Add(authTab);
        tabs.TabPages.Add(varsTab);
        tabs.TabPages.Add(preReqTab);
        tabs.TabPages.Add(testTab);

        // Buttons
        var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = AppTheme.Surface };
        var btnSave = CreateButton("Save", AppTheme.Accent, Color.White, new Point(Width - 170, 6));
        btnSave.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text)) { MessageBox.Show("Name is required."); return; }
            ResultCollection.Name = _txtName.Text;
            ResultCollection.Description = _txtDescription.Text;
            ResultCollection.Auth = _authEditor.GetAuth();
            ResultCollection.PreRequestScript = _preReqEditor.ScriptText;
            ResultCollection.TestScript = _testEditor.ScriptText;
            ResultCollection.Variables = _varEditor.GetItems().Select(i => new EnvironmentVariable
            { Key = i.Key, Value = i.Value, Enabled = i.Enabled, Description = i.Description }).ToList();
            DialogResult = DialogResult.OK;
            Close();
        };
        var btnCancel = CreateButton("Cancel", AppTheme.SurfaceElevated, AppTheme.TextPrimary, new Point(Width - 88, 6));
        btnCancel.FlatAppearance.BorderColor = AppTheme.Border;
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.Add(btnSave);
        btnPanel.Controls.Add(btnCancel);

        Controls.Add(tabs);
        Controls.Add(btnPanel);
        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private static void AddLabeledControl(Panel parent, string label, Control ctrl, int x, int y)
    {
        var lbl = new Label { Text = label, Location = new Point(x, y + 3), AutoSize = true, ForeColor = AppTheme.TextSecondary };
        ctrl.Location = new Point(x + 100, y);
        ctrl.BackColor = AppTheme.SurfaceElevated;
        ctrl.ForeColor = AppTheme.TextPrimary;
        if (ctrl is TextBox tb) tb.BorderStyle = BorderStyle.FixedSingle;
        parent.Controls.Add(lbl);
        parent.Controls.Add(ctrl);
    }

    private static Button CreateButton(string text, Color bg, Color fg, Point loc)
    {
        return new Button
        {
            Text = text, Location = loc, Width = 75, Height = 28,
            BackColor = bg, ForeColor = fg, FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
    }
}
