using Dakia.Controls;
using Dakia.Models;
using Dakia.Services;
using Dakia.Theme;

namespace Dakia.Forms;

public sealed class SettingsForm : Form
{
    private readonly WorkspaceSettings _settings;
    private readonly TabControl _tabs;

    // General
    private readonly CheckBox _chkSsl;
    private readonly CheckBox _chkRedirects;
    private readonly NumericUpDown _numTimeout;
    private readonly CheckBox _chkAutoSave;
    private readonly CheckBox _chkEnableScripts;
    private readonly CheckBox _chkCookies;
    private readonly CheckBox _chkStoreCookies;
    private readonly CheckBox _chkEncodeParams;

    // Proxy
    private readonly CheckBox _chkUseProxy;
    private readonly TextBox _txtProxyHost;
    private readonly NumericUpDown _numProxyPort;
    private readonly TextBox _txtProxyUser;
    private readonly TextBox _txtProxyPass;

    // Editor
    private readonly NumericUpDown _numFontSize;
    private readonly ComboBox _cmbFont;

    // Global Variables
    private readonly KeyValueEditorControl _globalVarsEditor;

    // History
    private readonly NumericUpDown _numHistoryMax;
    private readonly Button _btnClearHistory;

    public SettingsForm(WorkspaceSettings settings)
    {
        _settings = settings;
        _tabs = new TabControl();
        _chkSsl = new CheckBox();
        _chkRedirects = new CheckBox();
        _numTimeout = new NumericUpDown();
        _chkAutoSave = new CheckBox();
        _chkEnableScripts = new CheckBox();
        _chkCookies = new CheckBox();
        _chkStoreCookies = new CheckBox();
        _chkEncodeParams = new CheckBox();
        _chkUseProxy = new CheckBox();
        _txtProxyHost = new TextBox();
        _numProxyPort = new NumericUpDown();
        _txtProxyUser = new TextBox();
        _txtProxyPass = new TextBox();
        _numFontSize = new NumericUpDown();
        _cmbFont = new ComboBox();
        _globalVarsEditor = new KeyValueEditorControl();
        _numHistoryMax = new NumericUpDown();
        _btnClearHistory = new Button();
        InitForm();
    }

    private void InitForm()
    {
        Text = "Settings";
        Size = new Size(640, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        MinimizeBox = false;

        _tabs.Dock = DockStyle.Fill;
        _tabs.BackColor = AppTheme.Background;
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.ItemSize = new Size(110, 26);
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.DrawItem += (_, e) =>
        {
            e.Graphics.FillRectangle(new SolidBrush(e.Index == _tabs.SelectedIndex ? AppTheme.Background : AppTheme.Surface), e.Bounds);
            TextRenderer.DrawText(e.Graphics, _tabs.TabPages[e.Index].Text, AppTheme.FontNormal, e.Bounds,
                e.Index == _tabs.SelectedIndex ? AppTheme.TextPrimary : AppTheme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        BuildGeneralTab();
        BuildProxyTab();
        BuildEditorTab();
        BuildGlobalsTab();
        BuildAdvancedTab();

        // Button panel
        var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = AppTheme.Surface };
        var btnSave = new Button
        {
            Text = "Save",
            Location = new Point(Width - 170, 7),
            Width = 80, Height = 28, BackColor = AppTheme.Accent, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Right | AnchorStyles.Top, Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (_, _) => { SaveSettings(); DialogResult = DialogResult.OK; Close(); };

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(Width - 84, 7),
            Width = 70, Height = 28, BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Right | AnchorStyles.Top, Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = AppTheme.Border;
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.Add(btnSave);
        btnPanel.Controls.Add(btnCancel);

        Controls.Add(_tabs);
        Controls.Add(btnPanel);
        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void BuildGeneralTab()
    {
        var tp = new TabPage("General") { BackColor = AppTheme.Background };
        int y = 12;

        AddCheckbox(tp, _chkSsl, "SSL Certificate Verification", _settings.SslVerification, ref y);
        AddCheckbox(tp, _chkRedirects, "Follow Redirects", _settings.FollowRedirects, ref y);
        AddCheckbox(tp, _chkAutoSave, "Auto-save requests to collection", _settings.AutoSaveRequests, ref y);
        AddCheckbox(tp, _chkEnableScripts, "Enable Pre-request & Test scripts", _settings.EnableScripts, ref y);
        AddCheckbox(tp, _chkCookies, "Send Cookies", _settings.SendCookies, ref y);
        AddCheckbox(tp, _chkStoreCookies, "Store Cookies", _settings.StoreCookies, ref y);
        AddCheckbox(tp, _chkEncodeParams, "Encode Query Parameters", _settings.EncodeQueryParams, ref y);
        y += 8;

        AddNumericField(tp, "Request Timeout (ms):", _numTimeout, _settings.TimeoutMs, 100, 600000, ref y);
        AddNumericField(tp, "Max History Items:", _numHistoryMax, _settings.MaxHistoryItems, 10, 10000, ref y);

        _tabs.TabPages.Add(tp);
    }

    private void BuildProxyTab()
    {
        var tp = new TabPage("Proxy") { BackColor = AppTheme.Background };
        int y = 12;

        AddCheckbox(tp, _chkUseProxy, "Use Proxy", _settings.UseProxy, ref y);
        AddTextField(tp, "Proxy Host:", _txtProxyHost, _settings.ProxyHost, ref y);
        AddNumericField(tp, "Proxy Port:", _numProxyPort, _settings.ProxyPort, 1, 65535, ref y);
        AddTextField(tp, "Username:", _txtProxyUser, _settings.ProxyUsername, ref y);
        var lbl = new Label { Text = "Password:", Location = new Point(12, y + 3), Width = 120, ForeColor = AppTheme.TextSecondary };
        _txtProxyPass.Location = new Point(140, y);
        _txtProxyPass.Width = 250;
        _txtProxyPass.Text = _settings.ProxyPassword;
        _txtProxyPass.UseSystemPasswordChar = true;
        StyleTextBox(_txtProxyPass);
        tp.Controls.Add(lbl);
        tp.Controls.Add(_txtProxyPass);

        _tabs.TabPages.Add(tp);
    }

    private void BuildEditorTab()
    {
        var tp = new TabPage("Editor") { BackColor = AppTheme.Background };
        int y = 12;

        var lblFont = new Label { Text = "Editor Font:", Location = new Point(12, y + 3), Width = 120, ForeColor = AppTheme.TextSecondary };
        _cmbFont.Location = new Point(140, y);
        _cmbFont.Width = 200;
        _cmbFont.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbFont.BackColor = AppTheme.SurfaceElevated;
        _cmbFont.ForeColor = AppTheme.TextPrimary;
        _cmbFont.FlatStyle = FlatStyle.Flat;
        _cmbFont.Items.AddRange(["Consolas", "Courier New", "Lucida Console", "Monaco", "Source Code Pro"]);
        _cmbFont.SelectedItem = _settings.EditorFont;
        if (_cmbFont.SelectedIndex < 0) _cmbFont.SelectedIndex = 0;
        tp.Controls.Add(lblFont);
        tp.Controls.Add(_cmbFont);
        y += 32;

        AddNumericField(tp, "Font Size:", _numFontSize, _settings.FontSize, 8, 24, ref y);

        _tabs.TabPages.Add(tp);
    }

    private void BuildGlobalsTab()
    {
        var tp = new TabPage("Global Variables") { BackColor = AppTheme.Background };
        var lbl = new Label { Dock = DockStyle.Top, Height = 24, Text = "Global variables are available in all collections and environments.", ForeColor = AppTheme.TextSecondary, Padding = new Padding(8, 4, 0, 0) };
        _globalVarsEditor.Dock = DockStyle.Fill;
        _globalVarsEditor.LoadItems(_settings.GlobalVariables.Select(kv => new Models.KeyValueItem { Key = kv.Key, Value = kv.Value, Enabled = true }).ToList());
        tp.Controls.Add(_globalVarsEditor);
        tp.Controls.Add(lbl);
        _tabs.TabPages.Add(tp);
    }

    private void BuildAdvancedTab()
    {
        var tp = new TabPage("Advanced") { BackColor = AppTheme.Background };
        int y = 12;

        var lblDataDir = new Label { Text = "Data Directory:", Location = new Point(12, y + 3), Width = 120, ForeColor = AppTheme.TextSecondary };
        var lblPath = new Label
        {
            Text = StorageService.AppDataPath, Location = new Point(140, y + 3),
            AutoSize = true, ForeColor = AppTheme.Info, Cursor = Cursors.Hand
        };
        lblPath.Click += (_, _) => System.Diagnostics.Process.Start("explorer.exe", StorageService.AppDataPath);
        tp.Controls.Add(lblDataDir);
        tp.Controls.Add(lblPath);
        y += 32;

        _btnClearHistory.Text = "Clear Request History";
        _btnClearHistory.Location = new Point(12, y);
        _btnClearHistory.Width = 160;
        _btnClearHistory.Height = 28;
        _btnClearHistory.BackColor = AppTheme.SurfaceElevated;
        _btnClearHistory.ForeColor = AppTheme.Error;
        _btnClearHistory.FlatStyle = FlatStyle.Flat;
        _btnClearHistory.FlatAppearance.BorderColor = AppTheme.Error;
        _btnClearHistory.Cursor = Cursors.Hand;
        _btnClearHistory.Click += (_, _) =>
        {
            if (MessageBox.Show("Clear all request history?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                StorageService.Instance.ClearHistory();
                MessageBox.Show("History cleared.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
        tp.Controls.Add(_btnClearHistory);

        _tabs.TabPages.Add(tp);
    }

    private void SaveSettings()
    {
        _settings.SslVerification = _chkSsl.Checked;
        _settings.FollowRedirects = _chkRedirects.Checked;
        _settings.AutoSaveRequests = _chkAutoSave.Checked;
        _settings.EnableScripts = _chkEnableScripts.Checked;
        _settings.SendCookies = _chkCookies.Checked;
        _settings.StoreCookies = _chkStoreCookies.Checked;
        _settings.EncodeQueryParams = _chkEncodeParams.Checked;
        _settings.TimeoutMs = (int)_numTimeout.Value;
        _settings.MaxHistoryItems = (int)_numHistoryMax.Value;
        _settings.UseProxy = _chkUseProxy.Checked;
        _settings.ProxyHost = _txtProxyHost.Text;
        _settings.ProxyPort = (int)_numProxyPort.Value;
        _settings.ProxyUsername = _txtProxyUser.Text;
        _settings.ProxyPassword = _txtProxyPass.Text;
        _settings.EditorFont = _cmbFont.SelectedItem?.ToString() ?? "Consolas";
        _settings.FontSize = (int)_numFontSize.Value;
        _settings.GlobalVariables = _globalVarsEditor.GetItems()
            .Where(i => !string.IsNullOrEmpty(i.Key))
            .ToDictionary(i => i.Key, i => i.Value);
        StorageService.Instance.SaveSettings(_settings);
    }

    private static void AddCheckbox(Panel parent, CheckBox chk, string text, bool value, ref int y)
    {
        chk.Text = text;
        chk.Location = new Point(12, y);
        chk.AutoSize = true;
        chk.Checked = value;
        chk.BackColor = Color.Transparent;
        chk.ForeColor = AppTheme.TextPrimary;
        parent.Controls.Add(chk);
        y += 28;
    }

    private static void AddTextField(Panel parent, string label, TextBox tb, string value, ref int y)
    {
        var lbl = new Label { Text = label, Location = new Point(12, y + 3), Width = 120, ForeColor = AppTheme.TextSecondary };
        tb.Location = new Point(140, y);
        tb.Width = 250;
        tb.Text = value;
        StyleTextBox(tb);
        parent.Controls.Add(lbl);
        parent.Controls.Add(tb);
        y += 32;
    }

    private static void AddNumericField(Panel parent, string label, NumericUpDown num, decimal value, decimal min, decimal max, ref int y)
    {
        var lbl = new Label { Text = label, Location = new Point(12, y + 3), Width = 120, ForeColor = AppTheme.TextSecondary };
        num.Location = new Point(140, y);
        num.Width = 120;
        num.Minimum = min;
        num.Maximum = max;
        num.Value = Math.Clamp(value, min, max);
        num.BackColor = AppTheme.SurfaceElevated;
        num.ForeColor = AppTheme.TextPrimary;
        parent.Controls.Add(lbl);
        parent.Controls.Add(num);
        y += 32;
    }

    private static void StyleTextBox(TextBox tb)
    {
        tb.BackColor = AppTheme.SurfaceElevated;
        tb.ForeColor = AppTheme.TextPrimary;
        tb.BorderStyle = BorderStyle.FixedSingle;
    }
}
