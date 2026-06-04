using Dakia.Models;
using Dakia.Theme;

namespace Dakia.Forms;

public sealed class RequestEditForm : Form
{
    private readonly TextBox _txtName;
    private readonly TextBox _txtDescription;
    private readonly ComboBox _cmbMethod;
    private readonly TextBox _txtUrl;
    private readonly CheckBox _chkOpenInTab;

    public CollectionItem ResultItem { get; private set; }
    public bool OpenInTab { get; private set; }

    private static readonly string[] Methods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "CONNECT", "TRACE"];

    public RequestEditForm(CollectionItem? existing, DakiaCollection col, CollectionItem? parent)
    {
        ResultItem = existing ?? new CollectionItem { Type = "request", Request = new ApiRequest() };
        _txtName = new TextBox();
        _txtDescription = new TextBox();
        _cmbMethod = new ComboBox();
        _txtUrl = new TextBox();
        _chkOpenInTab = new CheckBox();
        InitForm(existing == null);
    }

    private void InitForm(bool isNew)
    {
        Text = isNew ? "New Request" : "Edit Request";
        Size = new Size(480, 280);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        int y = 16;

        AddField("Name:", _txtName, ref y);
        _txtName.Text = ResultItem.Name;
        _txtName.Width = 320;

        AddField("Description:", _txtDescription, ref y);
        _txtDescription.Text = ResultItem.Description;
        _txtDescription.Width = 320;

        var lbl3 = new Label { Text = "Method:", Location = new Point(12, y + 3), AutoSize = true, ForeColor = AppTheme.TextSecondary };
        _cmbMethod.Location = new Point(120, y);
        _cmbMethod.Width = 100;
        _cmbMethod.BackColor = AppTheme.SurfaceElevated;
        _cmbMethod.ForeColor = AppTheme.TextPrimary;
        _cmbMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbMethod.FlatStyle = FlatStyle.Flat;
        _cmbMethod.Items.AddRange(Methods);
        var methodIdx = Array.IndexOf(Methods, ResultItem.Request?.Method ?? "GET");
        _cmbMethod.SelectedIndex = methodIdx < 0 ? 0 : methodIdx;
        Controls.Add(lbl3);
        Controls.Add(_cmbMethod);
        y += 32;

        AddField("URL:", _txtUrl, ref y);
        _txtUrl.Text = ResultItem.Request?.Url ?? "";
        _txtUrl.Width = 320;
        _txtUrl.Font = AppTheme.FontMonoSmall;

        if (isNew)
        {
            _chkOpenInTab.Text = "Open request in a new tab";
            _chkOpenInTab.Location = new Point(12, y);
            _chkOpenInTab.AutoSize = true;
            _chkOpenInTab.Checked = true;
            _chkOpenInTab.BackColor = Color.Transparent;
            _chkOpenInTab.ForeColor = AppTheme.TextSecondary;
            Controls.Add(_chkOpenInTab);
            y += 26;
        }

        var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = AppTheme.Surface };
        var btnSave = new Button
        {
            Text = isNew ? "Create" : "Save",
            Location = new Point(Width - 170, 6),
            Width = 75, Height = 28,
            BackColor = AppTheme.Accent, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text)) { MessageBox.Show("Name is required."); return; }
            ResultItem.Name = _txtName.Text;
            ResultItem.Description = _txtDescription.Text;
            ResultItem.Request ??= new ApiRequest();
            ResultItem.Request.Method = _cmbMethod.SelectedItem?.ToString() ?? "GET";
            ResultItem.Request.Url = _txtUrl.Text;
            OpenInTab = _chkOpenInTab.Checked;
            DialogResult = DialogResult.OK;
            Close();
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(Width - 88, 6),
            Width = 75, Height = 28,
            BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderColor = AppTheme.Border;
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.Add(btnSave);
        btnPanel.Controls.Add(btnCancel);
        Controls.Add(btnPanel);
        AcceptButton = btnSave;
        CancelButton = btnCancel;

        _txtName.Focus();
    }

    private void AddField(string label, TextBox tb, ref int y)
    {
        var lbl = new Label { Text = label, Location = new Point(12, y + 3), AutoSize = true, ForeColor = AppTheme.TextSecondary };
        tb.Location = new Point(120, y);
        tb.BackColor = AppTheme.SurfaceElevated;
        tb.ForeColor = AppTheme.TextPrimary;
        tb.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(lbl);
        Controls.Add(tb);
        y += 32;
    }
}
