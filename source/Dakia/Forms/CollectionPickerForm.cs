using Dakia.Models;
using Dakia.Theme;

namespace Dakia.Forms;

public sealed class CollectionPickerForm : Form
{
    private readonly ListBox _list;
    public DakiaCollection? Selected { get; private set; }

    public CollectionPickerForm(List<DakiaCollection> collections)
    {
        _list = new ListBox();
        Text = "Select Collection";
        Size = new Size(360, 300);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.TextPrimary;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;

        var lbl = new Label { Text = "Select a collection to export:", Location = new Point(12, 12), AutoSize = true, ForeColor = AppTheme.TextSecondary };

        _list.Location = new Point(12, 32);
        _list.Size = new Size(320, 180);
        _list.BackColor = AppTheme.SurfaceElevated;
        _list.ForeColor = AppTheme.TextPrimary;
        _list.BorderStyle = BorderStyle.FixedSingle;
        foreach (var col in collections) _list.Items.Add(col.Name);
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;

        var btnOk = new Button { Text = "Export", Location = new Point(170, 226), Width = 75, Height = 28, BackColor = AppTheme.Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (_, _) =>
        {
            if (_list.SelectedIndex >= 0) Selected = collections[_list.SelectedIndex];
        };

        var btnCancel = new Button { Text = "Cancel", Location = new Point(252, 226), Width = 75, Height = 28, BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextPrimary, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
        btnCancel.FlatAppearance.BorderColor = AppTheme.Border;

        Controls.Add(lbl);
        Controls.Add(_list);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
