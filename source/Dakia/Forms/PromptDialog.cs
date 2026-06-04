using Dakia.Theme;

namespace Dakia.Forms;

public static class PromptDialog
{
    public static string? Show(string title, string prompt, string defaultValue = "")
    {
        using var form = new Form
        {
            Text = title,
            Size = new Size(400, 140),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lbl = new Label { Text = prompt, Location = new Point(12, 12), AutoSize = true, ForeColor = AppTheme.TextSecondary };
        var tb = new TextBox
        {
            Location = new Point(12, 32),
            Width = 360,
            Text = defaultValue,
            BackColor = AppTheme.SurfaceElevated,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnOk = new Button
        {
            Text = "OK",
            Location = new Point(220, 68),
            Width = 75,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderSize = 0;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(302, 68),
            Width = 75,
            BackColor = AppTheme.SurfaceElevated,
            ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = AppTheme.Border;

        form.Controls.Add(lbl);
        form.Controls.Add(tb);
        form.Controls.Add(btnOk);
        form.Controls.Add(btnCancel);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;

        tb.Focus();
        tb.SelectAll();

        return form.ShowDialog() == DialogResult.OK ? tb.Text : null;
    }
}
