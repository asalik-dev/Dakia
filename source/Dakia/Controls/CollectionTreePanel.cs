using Dakia.Models;
using Dakia.Services;
using Dakia.Theme;

namespace Dakia.Controls;

public sealed class CollectionTreePanel : UserControl
{
    private readonly TextBox _searchBox;
    private readonly TreeView _tree;
    private readonly Panel _header;
    private readonly TabControl _sidebarTabs;
    private readonly ListBox _historyList;
    private readonly Button _btnNewCollection;
    private readonly Button _btnImport;

    private List<DakiaCollection> _collections = [];
    private List<RequestHistory> _history = [];
    private string _searchText = "";

    public event EventHandler<CollectionItem>? RequestOpened;
    public event EventHandler<DakiaCollection>? CollectionSelected;
    public event EventHandler<DakiaCollection>? CollectionEdit;
    public event EventHandler<DakiaCollection>? CollectionDelete;
    public event EventHandler<(DakiaCollection Col, CollectionItem Item)>? ItemEdit;
    public event EventHandler<(DakiaCollection Col, CollectionItem Item)>? ItemDelete;
    public event EventHandler<(DakiaCollection Col, CollectionItem? Parent)>? AddRequest;
    public event EventHandler<(DakiaCollection Col, CollectionItem? Parent)>? AddFolder;
    public event EventHandler? NewCollectionClicked;
    public event EventHandler? ImportClicked;
    public event EventHandler<RequestHistory>? HistoryItemOpened;
    public event EventHandler? ClearHistoryClicked;

    public CollectionTreePanel()
    {
        _searchBox = new TextBox();
        _tree = new TreeView();
        _header = new Panel();
        _sidebarTabs = new TabControl();
        _historyList = new ListBox();
        _btnNewCollection = new Button();
        _btnImport = new Button();
        InitComponent();
    }

    private void InitComponent()
    {
        BackColor = AppTheme.SidebarBg;

        // Header
        _header.Dock = DockStyle.Top;
        _header.Height = 38;
        _header.BackColor = AppTheme.SidebarBg;

        _btnNewCollection.Text = "+ New";
        _btnNewCollection.Width = 58;
        _btnNewCollection.Height = 26;
        _btnNewCollection.Location = new Point(4, 6);
        _btnNewCollection.BackColor = AppTheme.Accent;
        _btnNewCollection.ForeColor = Color.White;
        _btnNewCollection.FlatStyle = FlatStyle.Flat;
        _btnNewCollection.FlatAppearance.BorderSize = 0;
        _btnNewCollection.Cursor = Cursors.Hand;
        _btnNewCollection.Font = AppTheme.FontBold;
        _btnNewCollection.Click += (_, _) => NewCollectionClicked?.Invoke(this, EventArgs.Empty);

        _btnImport.Text = "Import";
        _btnImport.Width = 58;
        _btnImport.Height = 26;
        _btnImport.Location = new Point(68, 6);
        _btnImport.BackColor = AppTheme.SurfaceElevated;
        _btnImport.ForeColor = AppTheme.TextPrimary;
        _btnImport.FlatStyle = FlatStyle.Flat;
        _btnImport.FlatAppearance.BorderColor = AppTheme.Border;
        _btnImport.Cursor = Cursors.Hand;
        _btnImport.Click += (_, _) => ImportClicked?.Invoke(this, EventArgs.Empty);

        _header.Controls.Add(_btnNewCollection);
        _header.Controls.Add(_btnImport);

        // Search box
        _searchBox.Dock = DockStyle.Top;
        _searchBox.Height = 26;
        _searchBox.BackColor = AppTheme.SurfaceElevated;
        _searchBox.ForeColor = AppTheme.TextSecondary;
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.Text = "Search...";
        _searchBox.GotFocus += (_, _) => { if (_searchBox.Text == "Search...") { _searchBox.Text = ""; _searchBox.ForeColor = AppTheme.TextPrimary; } };
        _searchBox.LostFocus += (_, _) => { if (string.IsNullOrEmpty(_searchBox.Text)) { _searchBox.Text = "Search..."; _searchBox.ForeColor = AppTheme.TextSecondary; } };
        _searchBox.TextChanged += (_, _) =>
        {
            _searchText = _searchBox.Text == "Search..." ? "" : _searchBox.Text;
            Refresh();
        };

        // Tabs
        _sidebarTabs.Dock = DockStyle.Fill;
        _sidebarTabs.BackColor = AppTheme.SidebarBg;
        _sidebarTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _sidebarTabs.ItemSize = new Size(90, 26);
        _sidebarTabs.SizeMode = TabSizeMode.Fixed;
        _sidebarTabs.DrawItem += DrawSidebarTab;

        // Collections tab
        var tpCollections = new TabPage("Collections") { BackColor = AppTheme.SidebarBg };
        _tree.Dock = DockStyle.Fill;
        _tree.BackColor = AppTheme.SidebarBg;
        _tree.ForeColor = AppTheme.TextPrimary;
        _tree.LineColor = AppTheme.Border;
        _tree.BorderStyle = BorderStyle.None;
        _tree.ShowRootLines = true;
        _tree.ShowLines = true;
        _tree.ShowPlusMinus = true;
        _tree.HideSelection = false;
        _tree.HotTracking = true;
        _tree.FullRowSelect = false;
        _tree.NodeMouseDoubleClick += OnNodeDoubleClick;
        _tree.NodeMouseClick += OnNodeClick;
        _tree.MouseDown += OnTreeMouseDown;
        _tree.DrawMode = TreeViewDrawMode.OwnerDrawText;
        _tree.DrawNode += OnDrawNode;
        tpCollections.Controls.Add(_tree);

        // History tab
        var tpHistory = new TabPage("History") { BackColor = AppTheme.SidebarBg };
        BuildHistoryTab(tpHistory);

        _sidebarTabs.TabPages.Add(tpCollections);
        _sidebarTabs.TabPages.Add(tpHistory);

        Controls.Add(_sidebarTabs);
        Controls.Add(_searchBox);
        Controls.Add(_header);
    }

    private void BuildHistoryTab(TabPage parent)
    {
        var toolbar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = AppTheme.SidebarBg };
        var btnClear = new Button
        {
            Text = "Clear All",
            Width = 70, Height = 22, Location = new Point(4, 4),
            BackColor = AppTheme.SurfaceElevated, ForeColor = AppTheme.TextSecondary,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        btnClear.FlatAppearance.BorderColor = AppTheme.Border;
        btnClear.Click += (_, _) => ClearHistoryClicked?.Invoke(this, EventArgs.Empty);
        toolbar.Controls.Add(btnClear);

        _historyList.Dock = DockStyle.Fill;
        _historyList.BackColor = AppTheme.SidebarBg;
        _historyList.ForeColor = AppTheme.TextPrimary;
        _historyList.BorderStyle = BorderStyle.None;
        _historyList.DrawMode = DrawMode.OwnerDrawVariable;
        _historyList.MeasureItem += (_, e) => { e.ItemHeight = 42; };
        _historyList.DrawItem += DrawHistoryItem;
        _historyList.MouseDoubleClick += (_, _) =>
        {
            if (_historyList.SelectedItem is RequestHistory h)
                HistoryItemOpened?.Invoke(this, h);
        };

        parent.Controls.Add(_historyList);
        parent.Controls.Add(toolbar);
    }

    private void DrawHistoryItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _history.Count) return;
        var h = _history[e.Index];
        var g = e.Graphics;
        var bounds = e.Bounds;

        g.FillRectangle(new SolidBrush((e.State & DrawItemState.Selected) != 0 ? AppTheme.SelectionBg : AppTheme.SidebarBg), bounds);

        // Method badge
        var methodColor = AppTheme.MethodColor(h.Request.Method);
        var methodText = h.Request.Method;
        using var mFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        var mSize = g.MeasureString(methodText, mFont);
        var mRect = new RectangleF(bounds.X + 6, bounds.Y + 6, mSize.Width + 8, 16);
        g.FillRectangle(new SolidBrush(Color.FromArgb(30, methodColor.R, methodColor.G, methodColor.B)), mRect);
        g.DrawString(methodText, mFont, new SolidBrush(methodColor), mRect.X + 4, mRect.Y + 1);

        // URL
        var urlRect = new RectangleF(bounds.X + 6, bounds.Y + 24, bounds.Width - 12, 14);
        var urlText = TruncateUrl(h.Request.Url, bounds.Width - 12, g, AppTheme.FontSmall);
        g.DrawString(urlText, AppTheme.FontSmall, new SolidBrush(AppTheme.TextSecondary), urlRect);

        // Time
        var timeStr = FormatTimeAgo(h.ExecutedAt);
        var timeSize = g.MeasureString(timeStr, AppTheme.FontSmall);
        g.DrawString(timeStr, AppTheme.FontSmall, new SolidBrush(AppTheme.TextDim),
            bounds.Right - timeSize.Width - 6, bounds.Y + 6);

        // Border
        g.DrawLine(new Pen(AppTheme.Border), bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
    }

    private static string TruncateUrl(string url, float maxWidth, Graphics g, Font font)
    {
        if (g.MeasureString(url, font).Width <= maxWidth) return url;
        while (url.Length > 4 && g.MeasureString(url + "...", font).Width > maxWidth)
            url = url[..^1];
        return url + "...";
    }

    private static string FormatTimeAgo(DateTime dt)
    {
        var diff = DateTime.UtcNow - dt;
        if (diff.TotalSeconds < 60) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return $"{(int)diff.TotalDays}d ago";
    }

    private void OnDrawNode(object? sender, DrawTreeNodeEventArgs e)
    {
        var node = e.Node;
        if (node == null) return;
        var g = e.Graphics;
        var bounds = e.Bounds;

        bool isSelected = (e.State & TreeNodeStates.Selected) != 0;
        bool isHot = (e.State & TreeNodeStates.Hot) != 0;

        g.FillRectangle(new SolidBrush(isSelected ? AppTheme.SelectionBg : isHot ? AppTheme.HoverBg : AppTheme.SidebarBg),
            new Rectangle(0, bounds.Y, _tree.Width, bounds.Height));

        if (node.Tag is DakiaCollection col)
        {
            var icon = node.IsExpanded ? "▼" : "▶";
            g.DrawString(icon, AppTheme.FontSmall, new SolidBrush(AppTheme.TextDim),
                bounds.X + 2, bounds.Y + 4);
            g.DrawString(col.Name, AppTheme.FontBold, new SolidBrush(AppTheme.TextPrimary),
                bounds.X + 16, bounds.Y + 3);
        }
        else if (node.Tag is CollectionItem item)
        {
            if (item.Type == "folder")
            {
                var icon = node.IsExpanded ? "▼ " : "▶ ";
                g.DrawString(icon + item.Name, AppTheme.FontNormal, new SolidBrush(AppTheme.TextSecondary),
                    bounds.X + 2, bounds.Y + 3);
            }
            else if (item.Request != null)
            {
                var method = item.Request.Method;
                var color = AppTheme.MethodColor(method);
                g.DrawString(method, AppTheme.FontMethod, new SolidBrush(color), bounds.X + 2, bounds.Y + 3);
                var methodWidth = (int)g.MeasureString(method.PadRight(7), AppTheme.FontMethod).Width;
                g.DrawString(item.Name, AppTheme.FontNormal, new SolidBrush(AppTheme.TextPrimary),
                    bounds.X + methodWidth + 4, bounds.Y + 3);
            }
        }
    }

    private void OnNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node?.Tag is CollectionItem item && item.Type == "request")
            RequestOpened?.Invoke(this, item);
    }

    private void OnNodeClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (e.Node?.Tag is DakiaCollection col)
                CollectionSelected?.Invoke(this, col);
        }
    }

    private void OnTreeMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        var node = _tree.GetNodeAt(e.Location);
        if (node == null) return;
        _tree.SelectedNode = node;
        ShowContextMenu(node, e.Location);
    }

    private void ShowContextMenu(TreeNode node, Point location)
    {
        var menu = new ContextMenuStrip
        {
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Renderer = new Theme.DarkToolStripRenderer()
        };

        if (node.Tag is DakiaCollection col)
        {
            menu.Items.Add("Add Request").Click += (_, _) => AddRequest?.Invoke(this, (col, null));
            menu.Items.Add("Add Folder").Click += (_, _) => AddFolder?.Invoke(this, (col, null));
            menu.Items.Add("-");
            menu.Items.Add("Edit Collection").Click += (_, _) => CollectionEdit?.Invoke(this, col);
            menu.Items.Add("Duplicate Collection").Click += (_, _) => DuplicateCollection(col);
            menu.Items.Add("-");
            menu.Items.Add("Export Collection").Click += (_, _) => ExportCollection(col);
            menu.Items.Add("-");
            menu.Items.Add("Delete Collection").Click += (_, _) =>
            {
                if (MessageBox.Show($"Delete collection '{col.Name}'?", "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    CollectionDelete?.Invoke(this, col);
            };
        }
        else if (node.Tag is CollectionItem item)
        {
            var parentCol = FindParentCollection(node);
            var parentItem = FindParentItem(node);
            if (parentCol == null) return;

            if (item.Type == "folder")
            {
                menu.Items.Add("Add Request").Click += (_, _) => AddRequest?.Invoke(this, (parentCol, item));
                menu.Items.Add("Add Folder").Click += (_, _) => AddFolder?.Invoke(this, (parentCol, item));
                menu.Items.Add("-");
            }

            menu.Items.Add("Edit").Click += (_, _) => ItemEdit?.Invoke(this, (parentCol, item));
            menu.Items.Add("Duplicate").Click += (_, _) => DuplicateItem(parentCol, item, parentItem);
            menu.Items.Add("-");
            menu.Items.Add("Delete").Click += (_, _) =>
            {
                if (MessageBox.Show($"Delete '{item.Name}'?", "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    ItemDelete?.Invoke(this, (parentCol, item));
            };
        }

        menu.Show(_tree, location);
    }

    private void DuplicateCollection(DakiaCollection col)
    {
        var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<DakiaCollection>(
            Newtonsoft.Json.JsonConvert.SerializeObject(col))!;
        copy.Id = Guid.NewGuid().ToString();
        copy.Name = $"{col.Name} (Copy)";
        StorageService.Instance.SaveCollection(copy);
        _collections.Add(copy);
        RefreshTree();
    }

    private void DuplicateItem(DakiaCollection col, CollectionItem item, CollectionItem? parent)
    {
        var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<CollectionItem>(
            Newtonsoft.Json.JsonConvert.SerializeObject(item))!;
        copy.Id = Guid.NewGuid().ToString();
        copy.Name = $"{item.Name} (Copy)";
        var target = parent?.Items ?? col.Items;
        var idx = target.IndexOf(item);
        target.Insert(idx + 1, copy);
        StorageService.Instance.SaveCollection(col);
        RefreshTree();
    }

    private void ExportCollection(DakiaCollection col)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Export Collection",
            FileName = $"{col.Name}.postman_collection.json",
            Filter = "Postman Collection|*.postman_collection.json|JSON|*.json"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var svc = new ImportExportService();
            File.WriteAllText(dlg.FileName, svc.ExportToPostmanV21(col));
            MessageBox.Show($"Collection exported to:\n{dlg.FileName}", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private DakiaCollection? FindParentCollection(TreeNode node)
    {
        var n = node;
        while (n != null)
        {
            if (n.Tag is DakiaCollection col) return col;
            n = n.Parent;
        }
        return null;
    }

    private CollectionItem? FindParentItem(TreeNode node)
    {
        if (node.Parent?.Tag is CollectionItem item) return item;
        return null;
    }

    public void LoadCollections(List<DakiaCollection> collections)
    {
        _collections = collections;
        RefreshTree();
    }

    public void LoadHistory(List<RequestHistory> history)
    {
        _history = history;
        RefreshHistory();
    }

    public void AddHistoryItem(RequestHistory item)
    {
        _history.Insert(0, item);
        RefreshHistory();
    }

    private void RefreshTree()
    {
        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        var filter = _searchText.ToLower();

        foreach (var col in _collections)
        {
            if (!string.IsNullOrEmpty(filter) && !ColMatchesFilter(col, filter)) continue;

            var colNode = new TreeNode(col.Name)
            {
                Tag = col,
                ForeColor = AppTheme.TextPrimary
            };

            AddItemNodes(colNode.Nodes, col.Items, filter);

            if (!string.IsNullOrEmpty(filter)) colNode.Expand();
            _tree.Nodes.Add(colNode);
        }

        _tree.EndUpdate();
    }

    private static bool ColMatchesFilter(DakiaCollection col, string filter)
    {
        if (col.Name.ToLower().Contains(filter)) return true;
        return ItemsMatchFilter(col.Items, filter);
    }

    private static bool ItemsMatchFilter(List<CollectionItem> items, string filter)
    {
        foreach (var item in items)
        {
            if (item.Name.ToLower().Contains(filter)) return true;
            if (item.Request?.Url.ToLower().Contains(filter) == true) return true;
            if (item.Type == "folder" && ItemsMatchFilter(item.Items, filter)) return true;
        }
        return false;
    }

    private static void AddItemNodes(TreeNodeCollection nodes, List<CollectionItem> items, string filter)
    {
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                if (!item.Name.ToLower().Contains(filter) &&
                    item.Request?.Url.ToLower().Contains(filter) != true &&
                    (item.Type != "folder" || !ItemsMatchFilter(item.Items, filter)))
                    continue;
            }

            var node = new TreeNode(item.Name) { Tag = item };
            if (item.Type == "folder" && item.Items != null)
                AddItemNodes(node.Nodes, item.Items, filter);
            nodes.Add(node);
        }
    }

    private void RefreshHistory()
    {
        _historyList.Items.Clear();
        foreach (var h in _history)
            _historyList.Items.Add(h);
    }

    public new void Refresh()
    {
        RefreshTree();
        RefreshHistory();
    }

    private void DrawSidebarTab(object? sender, DrawItemEventArgs e)
    {
        e.Graphics.FillRectangle(
            new SolidBrush(e.Index == _sidebarTabs.SelectedIndex ? AppTheme.SidebarBg : AppTheme.Background),
            e.Bounds);
        TextRenderer.DrawText(e.Graphics, _sidebarTabs.TabPages[e.Index].Text,
            AppTheme.FontNormal, e.Bounds,
            e.Index == _sidebarTabs.SelectedIndex ? AppTheme.TextPrimary : AppTheme.TextSecondary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
