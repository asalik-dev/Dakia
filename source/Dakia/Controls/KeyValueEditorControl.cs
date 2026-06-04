using Dakia.Models;
using Dakia.Theme;

namespace Dakia.Controls;

public sealed class KeyValueEditorControl : UserControl
{
    private readonly DataGridView _grid;
    private readonly Button _btnAdd;
    private readonly Button _btnDelete;
    private readonly CheckBox _chkAll;
    private readonly bool _showDescription;
    private bool _suppressEvents;

    public event EventHandler? DataChanged;

    public KeyValueEditorControl(bool showDescription = true)
    {
        _showDescription = showDescription;
        _grid = new DataGridView();
        _btnAdd = new Button();
        _btnDelete = new Button();
        _chkAll = new CheckBox();
        InitComponent();
    }

    private void InitComponent()
    {
        BackColor = AppTheme.Surface;

        // toolbar
        var toolbar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            BackColor = AppTheme.Surface,
            Padding = new Padding(4, 4, 4, 0)
        };

        _btnAdd.Text = "+ Add";
        _btnAdd.Width = 60;
        _btnAdd.Height = 22;
        _btnAdd.Location = new Point(4, 4);
        _btnAdd.BackColor = AppTheme.SurfaceElevated;
        _btnAdd.ForeColor = AppTheme.TextPrimary;
        _btnAdd.FlatStyle = FlatStyle.Flat;
        _btnAdd.FlatAppearance.BorderColor = AppTheme.Border;
        _btnAdd.Cursor = Cursors.Hand;
        _btnAdd.Click += (_, _) => AddRow();

        _btnDelete.Text = "Delete";
        _btnDelete.Width = 60;
        _btnDelete.Height = 22;
        _btnDelete.Location = new Point(70, 4);
        _btnDelete.BackColor = AppTheme.SurfaceElevated;
        _btnDelete.ForeColor = AppTheme.TextPrimary;
        _btnDelete.FlatStyle = FlatStyle.Flat;
        _btnDelete.FlatAppearance.BorderColor = AppTheme.Border;
        _btnDelete.Cursor = Cursors.Hand;
        _btnDelete.Click += (_, _) => DeleteSelected();

        toolbar.Controls.Add(_btnAdd);
        toolbar.Controls.Add(_btnDelete);

        // grid setup
        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = AppTheme.Surface;
        _grid.ForeColor = AppTheme.TextPrimary;
        _grid.GridColor = AppTheme.Border;
        _grid.BorderStyle = BorderStyle.None;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _grid.RowHeadersVisible = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.MultiSelect = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.ScrollBars = ScrollBars.Both;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _grid.RowTemplate.Height = 26;
        _grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

        // Colors
        _grid.DefaultCellStyle.BackColor = AppTheme.Surface;
        _grid.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        _grid.DefaultCellStyle.SelectionBackColor = AppTheme.SelectionBg;
        _grid.DefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;
        _grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SurfaceElevated;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextSecondary;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.SurfaceElevated;
        _grid.ColumnHeadersHeight = 26;
        _grid.EnableHeadersVisualStyles = false;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.SurfaceElevated;
        _grid.AlternatingRowsDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        _grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = AppTheme.SelectionBg;

        // Columns
        var colEnabled = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "",
            Width = 28,
            MinimumWidth = 28,
            Resizable = DataGridViewTriState.False,
            TrueValue = true,
            FalseValue = false
        };
        _grid.Columns.Add(colEnabled);

        var colKey = new DataGridViewTextBoxColumn
        {
            Name = "Key",
            HeaderText = "Key",
            Width = 200,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        _grid.Columns.Add(colKey);

        var colValue = new DataGridViewTextBoxColumn
        {
            Name = "Value",
            HeaderText = "Value",
            Width = 250,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        _grid.Columns.Add(colValue);

        if (_showDescription)
        {
            var colDesc = new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Description",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            _grid.Columns.Add(colDesc);
        }
        else
        {
            colValue.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        _grid.CellValueChanged += (_, _) => { if (!_suppressEvents) DataChanged?.Invoke(this, EventArgs.Empty); };
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewCheckBoxCell)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete && !_grid.IsCurrentCellInEditMode)
                DeleteSelected();
        };

        Controls.Add(_grid);
        Controls.Add(toolbar);
    }

    public void LoadItems(List<KeyValueItem> items)
    {
        _suppressEvents = true;
        _grid.Rows.Clear();
        foreach (var item in items)
        {
            var row = _grid.Rows[_grid.Rows.Add()];
            row.Cells["Enabled"].Value = item.Enabled;
            row.Cells["Key"].Value = item.Key;
            row.Cells["Value"].Value = item.Value;
            if (_showDescription) row.Cells["Description"].Value = item.Description;
        }
        _suppressEvents = false;
    }

    public List<KeyValueItem> GetItems()
    {
        var items = new List<KeyValueItem>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            var key = row.Cells["Key"].Value?.ToString() ?? "";
            var value = row.Cells["Value"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(value)) continue;
            items.Add(new KeyValueItem
            {
                Enabled = row.Cells["Enabled"].Value as bool? ?? true,
                Key = key,
                Value = value,
                Description = _showDescription ? row.Cells["Description"].Value?.ToString() ?? "" : ""
            });
        }
        return items;
    }

    private void AddRow()
    {
        var idx = _grid.Rows.Add();
        _grid.Rows[idx].Cells["Enabled"].Value = true;
        _grid.Rows[idx].Cells["Key"].Value = "";
        _grid.Rows[idx].Cells["Value"].Value = "";
        _grid.CurrentCell = _grid.Rows[idx].Cells["Key"];
        _grid.BeginEdit(true);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteSelected()
    {
        var toDelete = _grid.SelectedRows.Cast<DataGridViewRow>().ToList();
        foreach (var row in toDelete)
            _grid.Rows.Remove(row);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetBulkEditText(string text)
    {
        // Parse key:value lines
        _suppressEvents = true;
        _grid.Rows.Clear();
        foreach (var line in text.Split('\n'))
        {
            var l = line.Trim();
            if (string.IsNullOrEmpty(l)) continue;
            var idx = l.IndexOf(':');
            if (idx < 0) continue;
            var rowIdx = _grid.Rows.Add();
            _grid.Rows[rowIdx].Cells["Enabled"].Value = true;
            _grid.Rows[rowIdx].Cells["Key"].Value = l[..idx].Trim();
            _grid.Rows[rowIdx].Cells["Value"].Value = l[(idx + 1)..].Trim();
        }
        _suppressEvents = false;
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
