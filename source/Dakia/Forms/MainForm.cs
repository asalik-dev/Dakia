using Dakia.Controls;
using Dakia.Models;
using Dakia.Services;
using Dakia.Theme;

namespace Dakia.Forms;

public sealed class MainForm : Form
{
    // Layout
    private readonly SplitContainer _mainSplit;
    private readonly CollectionTreePanel _sidebar;
    private readonly TabControl _requestTabs;
    private readonly MenuStrip _menu;
    private readonly ToolStrip _toolbar;
    private readonly StatusStrip _statusBar;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly ToolStripStatusLabel _statusVersion;

    // Toolbar items
    private readonly ToolStripButton _btnNew;
    private readonly ToolStripButton _btnImport;
    private readonly ToolStripButton _btnRunner;
    private readonly ToolStripSeparator _sep1;
    private readonly ToolStripLabel _lblEnv;
    private readonly ToolStripComboBox _cmbEnvironment;
    private readonly ToolStripButton _btnManageEnvs;
    private readonly ToolStripButton _btnSettings;

    // State
    private WorkspaceSettings _settings;
    private List<DakiaCollection> _collections = [];
    private List<DakiaEnvironment> _environments = [];
    private DakiaEnvironment? _activeEnvironment;

    public MainForm()
    {
        _settings = StorageService.Instance.LoadSettings();

        _mainSplit = new SplitContainer();
        _sidebar = new CollectionTreePanel();
        _requestTabs = new TabControl();
        _menu = new MenuStrip();
        _toolbar = new ToolStrip();
        _statusBar = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _statusVersion = new ToolStripStatusLabel();
        _btnNew = new ToolStripButton();
        _btnImport = new ToolStripButton();
        _btnRunner = new ToolStripButton();
        _sep1 = new ToolStripSeparator();
        _lblEnv = new ToolStripLabel();
        _cmbEnvironment = new ToolStripComboBox();
        _btnManageEnvs = new ToolStripButton();
        _btnSettings = new ToolStripButton();

        InitWindow();
        InitMenu();
        InitToolbar();
        SetupLayout();
        InitStatusBar();
        WireSidebarEvents();
        WireTabEvents();

        LoadData();
        OpenNewRequestTab();

        ApplySettings();
    }

    private void InitWindow()
    {
        Text = "Dakia — API Client";
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);

        if (_settings.WindowMaximized)
            WindowState = FormWindowState.Maximized;
        else
        {
            ClientSize = new Size(_settings.WindowWidth, _settings.WindowHeight);
        }

        FormClosing += OnFormClosing;
        Resize += (_, _) => SaveWindowState();
    }

    private void InitMenu()
    {
        _menu.BackColor = AppTheme.Surface;
        _menu.ForeColor = AppTheme.TextPrimary;
        _menu.Renderer = new DarkToolStripRenderer();

        // File
        var mFile = AddMenu("&File");
        AddMenuItem(mFile, "New Request\tCtrl+N", (_, _) => OpenNewRequestTab(), "Ctrl+N");
        AddMenuItem(mFile, "New Collection", (_, _) => ShowNewCollectionDialog());
        mFile.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(mFile, "Import...\tCtrl+O", (_, _) => ImportCollection(), "Ctrl+O");
        AddMenuItem(mFile, "Export Collection...", (_, _) => ExportCurrentCollection());
        mFile.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(mFile, "Settings\tCtrl+,", (_, _) => ShowSettings(), "Ctrl+,");
        mFile.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(mFile, "Exit", (_, _) => Close());

        // Edit
        var mEdit = AddMenu("&Edit");
        AddMenuItem(mEdit, "Find in Collections\tCtrl+F", (_, _) => FocusSearch(), "Ctrl+F");

        // View
        var mView = AddMenu("&View");
        AddMenuItem(mView, "Toggle Sidebar", (_, _) => ToggleSidebar());
        AddMenuItem(mView, "Open Data Folder", (_, _) =>
            System.Diagnostics.Process.Start("explorer.exe", StorageService.AppDataPath));

        // Collections
        var mCols = AddMenu("&Collections");
        AddMenuItem(mCols, "New Collection", (_, _) => ShowNewCollectionDialog());
        AddMenuItem(mCols, "Import Collection...", (_, _) => ImportCollection());
        mCols.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(mCols, "Collection Runner", (_, _) => OpenCollectionRunner());

        // Environments
        var mEnvs = AddMenu("&Environments");
        AddMenuItem(mEnvs, "Manage Environments...", (_, _) => ShowEnvironmentManager());
        mEnvs.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(mEnvs, "No Environment", (_, _) => SetEnvironment(null));

        // Help
        var mHelp = AddMenu("&Help");
        AddMenuItem(mHelp, "About Dakia", (_, _) => ShowAbout());
        AddMenuItem(mHelp, "Keyboard Shortcuts", (_, _) => ShowShortcuts());

        Controls.Add(_menu);
        MainMenuStrip = _menu;
        KeyPreview = true;
        KeyDown += HandleGlobalKeys;
    }

    private ToolStripMenuItem AddMenu(string text)
    {
        var item = new ToolStripMenuItem(text)
        {
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary
        };
        _menu.Items.Add(item);
        return item;
    }

    private static void AddMenuItem(ToolStripMenuItem parent, string text, EventHandler onClick, string? shortcut = null)
    {
        var item = new ToolStripMenuItem(text)
        {
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary
        };
        if (shortcut != null)
        {
            if (Enum.TryParse<Keys>(shortcut.Replace("Ctrl+", "").Replace("Shift+", "").Replace("Alt+", ""), out var key))
            {
                var mods = Keys.None;
                if (shortcut.Contains("Ctrl")) mods |= Keys.Control;
                if (shortcut.Contains("Shift")) mods |= Keys.Shift;
                if (shortcut.Contains("Alt")) mods |= Keys.Alt;
                item.ShortcutKeys = mods | key;
            }
        }
        item.Click += onClick;
        parent.DropDownItems.Add(item);
    }

    private void InitToolbar()
    {
        _toolbar.BackColor = AppTheme.Surface;
        _toolbar.Renderer = new DarkToolStripRenderer();
        _toolbar.Padding = new Padding(4, 2, 4, 2);
        _toolbar.GripStyle = ToolStripGripStyle.Hidden;

        _btnNew.Text = "+ New";
        _btnNew.BackColor = AppTheme.Accent;
        _btnNew.ForeColor = Color.White;
        _btnNew.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnNew.Font = AppTheme.FontBold;
        _btnNew.Click += (_, _) => OpenNewRequestTab();
        _btnNew.AutoSize = false;
        _btnNew.Width = 58;
        _btnNew.Height = 26;
        _btnNew.Margin = new Padding(0, 0, 4, 0);

        _btnImport.Text = "Import";
        _btnImport.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnImport.Click += (_, _) => ImportCollection();
        _btnImport.AutoSize = false;
        _btnImport.Width = 60;
        _btnImport.Height = 26;

        _btnRunner.Text = "Runner";
        _btnRunner.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnRunner.Click += (_, _) => OpenCollectionRunner();
        _btnRunner.AutoSize = false;
        _btnRunner.Width = 60;
        _btnRunner.Height = 26;
        _btnRunner.Margin = new Padding(0, 0, 8, 0);

        _sep1.Margin = new Padding(4, 0, 4, 0);

        _lblEnv.Text = "Environment:";
        _lblEnv.ForeColor = AppTheme.TextSecondary;
        _lblEnv.Font = AppTheme.FontSmall;

        _cmbEnvironment.BackColor = AppTheme.SurfaceElevated;
        _cmbEnvironment.ForeColor = AppTheme.TextPrimary;
        _cmbEnvironment.Width = 160;
        _cmbEnvironment.AutoSize = false;
        _cmbEnvironment.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbEnvironment.Items.Add("No Environment");
        _cmbEnvironment.SelectedIndex = 0;
        _cmbEnvironment.SelectedIndexChanged += OnEnvironmentChanged;

        _btnManageEnvs.Text = "⚙ Envs";
        _btnManageEnvs.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnManageEnvs.Font = AppTheme.FontSmall;
        _btnManageEnvs.ForeColor = AppTheme.TextSecondary;
        _btnManageEnvs.Click += (_, _) => ShowEnvironmentManager();
        _btnManageEnvs.AutoSize = false;
        _btnManageEnvs.Width = 60;
        _btnManageEnvs.Height = 26;

        _btnSettings.Text = "⚙";
        _btnSettings.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnSettings.Alignment = ToolStripItemAlignment.Right;
        _btnSettings.Click += (_, _) => ShowSettings();
        _btnSettings.ToolTipText = "Settings";
        _btnSettings.ForeColor = AppTheme.TextSecondary;

        _toolbar.Items.Add(_btnNew);
        _toolbar.Items.Add(_btnImport);
        _toolbar.Items.Add(_btnRunner);
        _toolbar.Items.Add(_sep1);
        _toolbar.Items.Add(_lblEnv);
        _toolbar.Items.Add(_cmbEnvironment);
        _toolbar.Items.Add(_btnManageEnvs);
        _toolbar.Items.Add(_btnSettings);

        Controls.Add(_toolbar);
    }

    private void SetupLayout()
    {
        _mainSplit.Dock = DockStyle.Fill;
        _mainSplit.Orientation = Orientation.Vertical;
        _mainSplit.BackColor = AppTheme.Border;
        _mainSplit.SplitterWidth = 3;
        _mainSplit.Panel1MinSize = 200;
        _mainSplit.Panel2MinSize = 400;
        _mainSplit.SplitterDistance = _settings.SidebarWidth;

        _sidebar.Dock = DockStyle.Fill;
        _mainSplit.Panel1.BackColor = AppTheme.SidebarBg;
        _mainSplit.Panel1.Controls.Add(_sidebar);

        _requestTabs.Dock = DockStyle.Fill;
        _requestTabs.BackColor = AppTheme.Background;
        _requestTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _requestTabs.SizeMode = TabSizeMode.Normal;
        _requestTabs.Padding = new Point(12, 6);
        _requestTabs.DrawItem += DrawMainTab;
        _requestTabs.MouseDown += OnTabMouseDown;
        _requestTabs.Multiline = false;

        _mainSplit.Panel2.BackColor = AppTheme.Background;
        _mainSplit.Panel2.Controls.Add(_requestTabs);

        Controls.Add(_mainSplit);
    }

    private void InitStatusBar()
    {
        _statusBar.BackColor = AppTheme.Surface;
        _statusBar.SizingGrip = true;
        _statusBar.Renderer = new DarkToolStripRenderer();

        _statusLabel.Text = "Ready";
        _statusLabel.ForeColor = AppTheme.TextSecondary;
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        _statusVersion.Text = "Dakia v1.0.0";
        _statusVersion.ForeColor = AppTheme.TextDim;
        _statusVersion.Alignment = ToolStripItemAlignment.Right;

        _statusBar.Items.Add(_statusLabel);
        _statusBar.Items.Add(_statusVersion);
        Controls.Add(_statusBar);
    }

    private void WireSidebarEvents()
    {
        _sidebar.RequestOpened += (_, item) => OpenRequestTab(item, null);
        _sidebar.NewCollectionClicked += (_, _) => ShowNewCollectionDialog();
        _sidebar.ImportClicked += (_, _) => ImportCollection();
        _sidebar.CollectionEdit += (_, col) => ShowEditCollectionDialog(col);
        _sidebar.CollectionDelete += (_, col) => DeleteCollection(col);
        _sidebar.AddRequest += (_, args) => ShowAddRequestDialog(args.Col, args.Parent);
        _sidebar.AddFolder += (_, args) => ShowAddFolderDialog(args.Col, args.Parent);
        _sidebar.ItemEdit += (_, args) => ShowEditItemDialog(args.Col, args.Item);
        _sidebar.ItemDelete += (_, args) => DeleteItem(args.Col, args.Item);
        _sidebar.HistoryItemOpened += (_, h) => OpenHistoryTab(h);
        _sidebar.ClearHistoryClicked += (_, _) => ClearHistory();
    }

    private void WireTabEvents()
    {
        _requestTabs.SelectedIndexChanged += (_, _) => UpdateStatusForActiveTab();
    }

    // ── Data Loading ─────────────────────────────────────────────────────────

    private void LoadData()
    {
        _collections = StorageService.Instance.LoadAllCollections();
        _environments = StorageService.Instance.LoadAllEnvironments();
        var history = StorageService.Instance.LoadHistory();

        _sidebar.LoadCollections(_collections);
        _sidebar.LoadHistory(history);

        RefreshEnvironmentDropdown();

        var activeEnvId = _settings.ActiveEnvironmentId;
        if (!string.IsNullOrEmpty(activeEnvId))
        {
            var env = _environments.FirstOrDefault(e => e.Id == activeEnvId);
            if (env != null) SetEnvironmentDirect(env);
        }
    }

    private void RefreshEnvironmentDropdown()
    {
        _cmbEnvironment.Items.Clear();
        _cmbEnvironment.Items.Add("No Environment");
        foreach (var env in _environments)
            _cmbEnvironment.Items.Add(env.Name);

        if (_activeEnvironment != null)
        {
            var idx = _environments.IndexOf(_activeEnvironment) + 1;
            if (idx > 0) _cmbEnvironment.SelectedIndex = idx;
            else _cmbEnvironment.SelectedIndex = 0;
        }
        else
        {
            _cmbEnvironment.SelectedIndex = 0;
        }
    }

    // ── Tabs ──────────────────────────────────────────────────────────────────

    private void OpenNewRequestTab()
    {
        var panel = CreateRequestPanel();
        panel.LoadRequest(new ApiRequest());

        var tp = new TabPage("Untitled") { BackColor = AppTheme.Background, BorderStyle = BorderStyle.None };
        panel.Dock = DockStyle.Fill;
        tp.Controls.Add(panel);
        _requestTabs.TabPages.Add(tp);
        _requestTabs.SelectedTab = tp;

        panel.TitleChanged += (_, title) => tp.Text = title;

        SetStatus("New request");
    }

    public void OpenRequestTab(CollectionItem item, DakiaCollection? col)
    {
        if (item.Request == null) return;

        // Check if already open
        foreach (TabPage tp in _requestTabs.TabPages)
        {
            if (tp.Controls.Count > 0 && tp.Controls[0] is RequestPanel rp &&
                rp.BoundItem?.Id == item.Id)
            {
                _requestTabs.SelectedTab = tp;
                return;
            }
        }

        var panel = CreateRequestPanel();
        panel.BoundItem = item;
        panel.BoundCollection = col;
        if (col != null) panel.SetCollection(col);
        panel.LoadRequest(item.Request);

        var tabPage = new TabPage(item.Name) { BackColor = AppTheme.Background, BorderStyle = BorderStyle.None };
        panel.Dock = DockStyle.Fill;
        tabPage.Controls.Add(panel);
        _requestTabs.TabPages.Add(tabPage);
        _requestTabs.SelectedTab = tabPage;

        panel.TitleChanged += (_, title) => tabPage.Text = title;
    }

    private void OpenHistoryTab(RequestHistory h)
    {
        var panel = CreateRequestPanel();
        panel.LoadRequest(h.Request);

        var title = $"{h.Request.Method} (history)";
        var tp = new TabPage(title) { BackColor = AppTheme.Background, BorderStyle = BorderStyle.None };
        panel.Dock = DockStyle.Fill;
        tp.Controls.Add(panel);
        _requestTabs.TabPages.Add(tp);
        _requestTabs.SelectedTab = tp;

        if (h.Response != null)
        {
            // Cannot easily call internal method; response shown next request send
        }
    }

    private RequestPanel CreateRequestPanel()
    {
        var panel = new RequestPanel(_settings);
        panel.SetEnvironment(_activeEnvironment);
        panel.SetGlobals(_settings.GlobalVariables);
        panel.SetSplitDistance(_settings.ResponsePanelHeight > 0 ? _settings.ResponsePanelHeight : 280);
        return panel;
    }

    private void CloseTab(TabPage tp)
    {
        if (_requestTabs.TabPages.Count <= 1)
        {
            // Keep at least one tab; just clear it
            OpenNewRequestTab();
        }
        _requestTabs.TabPages.Remove(tp);
    }

    // ── Collection CRUD ───────────────────────────────────────────────────────

    private void ShowNewCollectionDialog()
    {
        using var dlg = new Forms.CollectionEditForm(null);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var col = dlg.ResultCollection;
        StorageService.Instance.SaveCollection(col);
        _collections.Add(col);
        _sidebar.LoadCollections(_collections);
        SetStatus($"Collection '{col.Name}' created.");
    }

    private void ShowEditCollectionDialog(DakiaCollection col)
    {
        using var dlg = new Forms.CollectionEditForm(col);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        StorageService.Instance.SaveCollection(col);
        _sidebar.LoadCollections(_collections);
        SetStatus($"Collection '{col.Name}' updated.");
    }

    private void DeleteCollection(DakiaCollection col)
    {
        StorageService.Instance.DeleteCollection(col.Id);
        _collections.Remove(col);
        _sidebar.LoadCollections(_collections);
        SetStatus($"Collection '{col.Name}' deleted.");
    }

    private void ShowAddRequestDialog(DakiaCollection col, CollectionItem? parent)
    {
        using var dlg = new Forms.RequestEditForm(null, col, parent);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var item = dlg.ResultItem;
        var target = parent?.Items ?? col.Items;
        target.Add(item);
        StorageService.Instance.SaveCollection(col);
        _sidebar.LoadCollections(_collections);

        if (dlg.OpenInTab)
            OpenRequestTab(item, col);
    }

    private void ShowAddFolderDialog(DakiaCollection col, CollectionItem? parent)
    {
        var name = PromptDialog.Show("New Folder", "Folder name:", "New Folder");
        if (string.IsNullOrEmpty(name)) return;

        var folder = new CollectionItem { Name = name, Type = "folder" };
        var target = parent?.Items ?? col.Items;
        target.Add(folder);
        StorageService.Instance.SaveCollection(col);
        _sidebar.LoadCollections(_collections);
    }

    private void ShowEditItemDialog(DakiaCollection col, CollectionItem item)
    {
        using var dlg = new Forms.RequestEditForm(item, col, null);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        StorageService.Instance.SaveCollection(col);
        _sidebar.LoadCollections(_collections);
    }

    private void DeleteItem(DakiaCollection col, CollectionItem item)
    {
        RemoveItemRecursive(col.Items, item);
        StorageService.Instance.SaveCollection(col);
        _sidebar.LoadCollections(_collections);
        SetStatus($"'{item.Name}' deleted.");
    }

    private static bool RemoveItemRecursive(List<CollectionItem> items, CollectionItem target)
    {
        if (items.Remove(target)) return true;
        foreach (var item in items)
            if (item.Type == "folder" && RemoveItemRecursive(item.Items, target)) return true;
        return false;
    }

    // ── Environments ──────────────────────────────────────────────────────────

    private void OnEnvironmentChanged(object? sender, EventArgs e)
    {
        var idx = _cmbEnvironment.SelectedIndex;
        SetEnvironment(idx <= 0 ? null : _environments[idx - 1]);
    }

    private void SetEnvironment(DakiaEnvironment? env)
    {
        SetEnvironmentDirect(env);
        RefreshEnvironmentDropdown();
    }

    private void SetEnvironmentDirect(DakiaEnvironment? env)
    {
        _activeEnvironment = env;
        _settings.ActiveEnvironmentId = env?.Id ?? "";
        StorageService.Instance.SaveSettings(_settings);

        foreach (TabPage tp in _requestTabs.TabPages)
        {
            if (tp.Controls.Count > 0 && tp.Controls[0] is RequestPanel rp)
                rp.SetEnvironment(env);
        }

        SetStatus(env != null ? $"Environment: {env.Name}" : "No environment selected");
    }

    private void ShowEnvironmentManager()
    {
        using var dlg = new Forms.EnvironmentManagerForm(_environments, _settings);
        dlg.ShowDialog();
        _environments = StorageService.Instance.LoadAllEnvironments();
        RefreshEnvironmentDropdown();

        // Re-apply active env (may have been renamed/deleted)
        if (_activeEnvironment != null)
        {
            _activeEnvironment = _environments.FirstOrDefault(e => e.Id == _activeEnvironment.Id) ?? null;
            SetEnvironmentDirect(_activeEnvironment);
        }
        _sidebar.LoadCollections(_collections);
    }

    // ── Import/Export ─────────────────────────────────────────────────────────

    private void ImportCollection()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Import Collection or Environment",
            Filter = "Postman Files|*.json;*.postman_collection.json|All JSON|*.json|All Files|*.*",
            Multiselect = true
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var svc = new ImportExportService();
        int imported = 0;
        foreach (var file in dlg.FileNames)
        {
            var json = File.ReadAllText(file);
            try
            {
                var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);
                if (jObj["_postman_variable_scope"] != null)
                {
                    var env = svc.ImportEnvironment(json);
                    if (env != null)
                    {
                        StorageService.Instance.SaveEnvironment(env);
                        _environments = StorageService.Instance.LoadAllEnvironments();
                        RefreshEnvironmentDropdown();
                        imported++;
                    }
                }
                else
                {
                    var col = svc.ImportPostmanCollection(json);
                    if (col == null)
                    {
                        // Try as raw Dakia format
                        col = StorageService.Instance.ImportCollection(file);
                    }
                    if (col != null)
                    {
                        col.Id = Guid.NewGuid().ToString();
                        StorageService.Instance.SaveCollection(col);
                        _collections.Add(col);
                        _sidebar.LoadCollections(_collections);
                        imported++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import {Path.GetFileName(file)}:\n{ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        if (imported > 0)
            SetStatus($"Imported {imported} item(s) successfully.");
    }

    private void ExportCurrentCollection()
    {
        if (_collections.Count == 0)
        {
            MessageBox.Show("No collections to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        // Let user pick collection
        using var picker = new Forms.CollectionPickerForm(_collections);
        if (picker.ShowDialog() != DialogResult.OK || picker.Selected == null) return;

        var col = picker.Selected;
        using var dlg = new SaveFileDialog
        {
            Title = "Export Collection",
            FileName = $"{col.Name}.postman_collection.json",
            Filter = "Postman Collection|*.postman_collection.json|JSON|*.json"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var svc = new ImportExportService();
        File.WriteAllText(dlg.FileName, svc.ExportToPostmanV21(col));
        SetStatus($"Exported to {dlg.FileName}");
    }

    // ── Collection Runner ─────────────────────────────────────────────────────

    private void OpenCollectionRunner()
    {
        using var dlg = new Forms.CollectionRunnerForm(_collections, _environments, _settings);
        dlg.ShowDialog();
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    private void ShowSettings()
    {
        using var dlg = new Forms.SettingsForm(_settings);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        _settings = StorageService.Instance.LoadSettings();
        ApplySettings();
    }

    private void ApplySettings()
    {
        foreach (TabPage tp in _requestTabs.TabPages)
        {
            if (tp.Controls.Count > 0 && tp.Controls[0] is RequestPanel rp)
                rp.SetGlobals(_settings.GlobalVariables);
        }
    }

    // ── Misc ──────────────────────────────────────────────────────────────────

    private void ClearHistory()
    {
        if (MessageBox.Show("Clear all request history?", "Confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            StorageService.Instance.ClearHistory();
            _sidebar.LoadHistory([]);
            SetStatus("History cleared.");
        }
    }

    private void ToggleSidebar()
    {
        _mainSplit.Panel1Collapsed = !_mainSplit.Panel1Collapsed;
    }

    private void FocusSearch()
    {
        _mainSplit.Panel1Collapsed = false;
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "Dakia — API Client v1.0.0\n\n" +
            "A production-ready Postman alternative.\n" +
            "Data stored in: " + StorageService.AppDataPath + "\n\n" +
            "Built with .NET 10 WinForms",
            "About Dakia", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowShortcuts()
    {
        MessageBox.Show(
            "Keyboard Shortcuts:\n\n" +
            "Ctrl+N      New Request Tab\n" +
            "Ctrl+O      Import Collection\n" +
            "Ctrl+S      Save Request\n" +
            "Ctrl+Enter  Send Request\n" +
            "Ctrl+W      Close Tab\n" +
            "Ctrl+F      Focus Search\n" +
            "Ctrl+,      Settings\n" +
            "F5          Send Request\n",
            "Keyboard Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void HandleGlobalKeys(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.N)
        {
            OpenNewRequestTab();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.W)
        {
            if (_requestTabs.SelectedTab != null)
                CloseTab(_requestTabs.SelectedTab);
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.S)
        {
            if (_requestTabs.SelectedTab?.Controls[0] is RequestPanel rp)
                rp.SaveToCollection();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F5)
        {
            // Trigger send in active tab — done via button, so just route focus
        }
    }

    private void OnTabMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            for (int i = 0; i < _requestTabs.TabCount; i++)
            {
                if (_requestTabs.GetTabRect(i).Contains(e.Location))
                {
                    CloseTab(_requestTabs.TabPages[i]);
                    break;
                }
            }
        }
        else if (e.Button == MouseButtons.Right)
        {
            for (int i = 0; i < _requestTabs.TabCount; i++)
            {
                if (_requestTabs.GetTabRect(i).Contains(e.Location))
                {
                    var tp = _requestTabs.TabPages[i];
                    ShowTabContextMenu(tp, e.Location);
                    break;
                }
            }
        }
    }

    private void ShowTabContextMenu(TabPage tp, Point location)
    {
        var menu = new ContextMenuStrip { BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Renderer = new DarkToolStripRenderer() };
        menu.Items.Add("Close Tab").Click += (_, _) => CloseTab(tp);
        menu.Items.Add("Close Other Tabs").Click += (_, _) =>
        {
            var others = _requestTabs.TabPages.Cast<TabPage>().Where(t => t != tp).ToList();
            foreach (var t in others) CloseTab(t);
        };
        menu.Items.Add("Close All Tabs").Click += (_, _) =>
        {
            var all = _requestTabs.TabPages.Cast<TabPage>().ToList();
            foreach (var t in all) _requestTabs.TabPages.Remove(t);
            OpenNewRequestTab();
        };
        menu.Items.Add("-");
        if (tp.Controls.Count > 0 && tp.Controls[0] is RequestPanel rp && rp.BoundItem != null)
            menu.Items.Add("Save Request").Click += (_, _) => rp.SaveToCollection();
        menu.Show(_requestTabs, location);
    }

    private void DrawMainTab(object? sender, DrawItemEventArgs e)
    {
        var selected = e.Index == _requestTabs.SelectedIndex;
        var bounds = e.Bounds;

        using var bgBrush = new SolidBrush(selected ? AppTheme.Background : AppTheme.Surface);
        e.Graphics.FillRectangle(bgBrush, bounds);

        if (selected)
        {
            using var accentPen = new Pen(AppTheme.Accent, 2);
            e.Graphics.DrawLine(accentPen, bounds.Left, bounds.Top, bounds.Right, bounds.Top);
        }

        // Close button (x) in top-right
        var text = _requestTabs.TabPages[e.Index].Text;
        var closeRect = new Rectangle(bounds.Right - 18, bounds.Top + (bounds.Height - 14) / 2, 14, 14);
        var textRect = new Rectangle(bounds.Left + 4, bounds.Top, bounds.Width - 22, bounds.Height);

        TextRenderer.DrawText(e.Graphics, text, AppTheme.FontSmall, textRect,
            selected ? AppTheme.TextPrimary : AppTheme.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (selected)
        {
            TextRenderer.DrawText(e.Graphics, "×", AppTheme.FontBold, closeRect,
                AppTheme.TextDim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    private void UpdateStatusForActiveTab()
    {
        if (_requestTabs.SelectedTab?.Controls[0] is RequestPanel rp)
            SetStatus(rp.TabTitle);
    }

    private void SetStatus(string msg) => _statusLabel.Text = msg;

    private void SaveWindowState()
    {
        if (WindowState == FormWindowState.Normal)
        {
            _settings.WindowWidth = ClientSize.Width;
            _settings.WindowHeight = ClientSize.Height;
        }
        _settings.WindowMaximized = WindowState == FormWindowState.Maximized;
        _settings.SidebarWidth = _mainSplit.SplitterDistance;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveWindowState();

        // Check for unsaved changes
        bool anyDirty = false;
        foreach (TabPage tp in _requestTabs.TabPages)
        {
            if (tp.Controls.Count > 0 && tp.Controls[0] is RequestPanel rp && rp.IsDirty && rp.BoundItem != null)
            {
                anyDirty = true;
                break;
            }
        }

        if (anyDirty)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Save before closing?",
                "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (result == DialogResult.Cancel) { e.Cancel = true; return; }
            if (result == DialogResult.Yes)
            {
                foreach (TabPage tp in _requestTabs.TabPages)
                    if (tp.Controls.Count > 0 && tp.Controls[0] is RequestPanel rp && rp.IsDirty)
                        rp.SaveToCollection();
            }
        }

        StorageService.Instance.SaveSettings(_settings);
    }
}
