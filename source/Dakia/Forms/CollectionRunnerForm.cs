using Dakia.Models;
using Dakia.Services;
using Dakia.Theme;

namespace Dakia.Forms;

public sealed class CollectionRunnerForm : Form
{
    private readonly ComboBox _cmbCollection;
    private readonly NumericUpDown _numIterations;
    private readonly NumericUpDown _numDelay;
    private readonly ComboBox _cmbEnvironment;
    private readonly CheckBox _chkSaveResponses;
    private readonly Button _btnRun;
    private readonly Button _btnStop;
    private readonly ListView _resultList;
    private readonly Label _lblSummary;
    private readonly ProgressBar _progress;

    private readonly List<DakiaCollection> _collections;
    private readonly List<DakiaEnvironment> _environments;
    private readonly WorkspaceSettings _settings;
    private CancellationTokenSource? _cts;

    public CollectionRunnerForm(List<DakiaCollection> collections, List<DakiaEnvironment> environments, WorkspaceSettings settings)
    {
        _collections = collections;
        _environments = environments;
        _settings = settings;
        _cmbCollection = new ComboBox();
        _numIterations = new NumericUpDown();
        _numDelay = new NumericUpDown();
        _cmbEnvironment = new ComboBox();
        _chkSaveResponses = new CheckBox();
        _btnRun = new Button();
        _btnStop = new Button();
        _resultList = new ListView();
        _lblSummary = new Label();
        _progress = new ProgressBar();
        InitForm();
    }

    private void InitForm()
    {
        Text = "Collection Runner";
        Size = new Size(800, 620);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        MinimizeBox = false;

        // Config panel
        var configPanel = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8) };

        int y = 10;
        AddConfigRow(configPanel, "Collection:", _cmbCollection, ref y);
        _cmbCollection.Width = 300;
        _cmbCollection.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbCollection.BackColor = AppTheme.SurfaceElevated;
        _cmbCollection.ForeColor = AppTheme.TextPrimary;
        _cmbCollection.Items.Add("-- Select Collection --");
        foreach (var c in _collections) _cmbCollection.Items.Add(c.Name);
        _cmbCollection.SelectedIndex = 0;

        AddConfigRow(configPanel, "Environment:", _cmbEnvironment, ref y);
        _cmbEnvironment.Width = 200;
        _cmbEnvironment.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbEnvironment.BackColor = AppTheme.SurfaceElevated;
        _cmbEnvironment.ForeColor = AppTheme.TextPrimary;
        _cmbEnvironment.Items.Add("No Environment");
        foreach (var e in _environments) _cmbEnvironment.Items.Add(e.Name);
        _cmbEnvironment.SelectedIndex = 0;

        // Iterations and delay on same line
        var lbl3 = new Label { Text = "Iterations:", Location = new Point(12, y + 3), Width = 80, ForeColor = AppTheme.TextSecondary };
        _numIterations.Location = new Point(100, y);
        _numIterations.Width = 70;
        _numIterations.Minimum = 1;
        _numIterations.Maximum = 10000;
        _numIterations.Value = 1;
        _numIterations.BackColor = AppTheme.SurfaceElevated;
        _numIterations.ForeColor = AppTheme.TextPrimary;

        var lbl4 = new Label { Text = "Delay (ms):", Location = new Point(185, y + 3), Width = 80, ForeColor = AppTheme.TextSecondary };
        _numDelay.Location = new Point(270, y);
        _numDelay.Width = 80;
        _numDelay.Minimum = 0;
        _numDelay.Maximum = 60000;
        _numDelay.Value = 0;
        _numDelay.Increment = 100;
        _numDelay.BackColor = AppTheme.SurfaceElevated;
        _numDelay.ForeColor = AppTheme.TextPrimary;

        configPanel.Controls.Add(lbl3);
        configPanel.Controls.Add(_numIterations);
        configPanel.Controls.Add(lbl4);
        configPanel.Controls.Add(_numDelay);

        _btnRun.Text = "▶ Run";
        _btnRun.BackColor = AppTheme.Accent;
        _btnRun.ForeColor = Color.White;
        _btnRun.FlatStyle = FlatStyle.Flat;
        _btnRun.FlatAppearance.BorderSize = 0;
        _btnRun.Width = 80;
        _btnRun.Height = 28;
        _btnRun.Location = new Point(440, y);
        _btnRun.Font = AppTheme.FontBold;
        _btnRun.Cursor = Cursors.Hand;
        _btnRun.Click += (_, _) => _ = RunCollectionAsync();
        configPanel.Controls.Add(_btnRun);

        _btnStop.Text = "■ Stop";
        _btnStop.BackColor = AppTheme.SurfaceElevated;
        _btnStop.ForeColor = AppTheme.Error;
        _btnStop.FlatStyle = FlatStyle.Flat;
        _btnStop.FlatAppearance.BorderColor = AppTheme.Error;
        _btnStop.Width = 80;
        _btnStop.Height = 28;
        _btnStop.Location = new Point(528, y);
        _btnStop.Enabled = false;
        _btnStop.Cursor = Cursors.Hand;
        _btnStop.Click += (_, _) => _cts?.Cancel();
        configPanel.Controls.Add(_btnStop);

        // Progress
        _progress.Dock = DockStyle.Top;
        _progress.Height = 4;
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.BackColor = AppTheme.Surface;
        _progress.ForeColor = AppTheme.Accent;
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.Value = 0;

        // Results
        _resultList.Dock = DockStyle.Fill;
        _resultList.BackColor = AppTheme.Surface;
        _resultList.ForeColor = AppTheme.TextPrimary;
        _resultList.BorderStyle = BorderStyle.None;
        _resultList.View = View.Details;
        _resultList.FullRowSelect = true;
        _resultList.GridLines = false;
        _resultList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _resultList.Columns.Add("#", 40);
        _resultList.Columns.Add("Request Name", 200);
        _resultList.Columns.Add("Method", 70);
        _resultList.Columns.Add("Status", 70);
        _resultList.Columns.Add("Time", 70);
        _resultList.Columns.Add("Size", 70);
        _resultList.Columns.Add("Tests", 80);
        _resultList.Columns.Add("Error", 200);

        _lblSummary.Dock = DockStyle.Bottom;
        _lblSummary.Height = 28;
        _lblSummary.BackColor = AppTheme.SurfaceElevated;
        _lblSummary.ForeColor = AppTheme.TextSecondary;
        _lblSummary.TextAlign = ContentAlignment.MiddleLeft;
        _lblSummary.Padding = new Padding(12, 0, 0, 0);
        _lblSummary.Text = "Ready to run";

        Controls.Add(_resultList);
        Controls.Add(_progress);
        Controls.Add(configPanel);
        Controls.Add(_lblSummary);
    }

    private async Task RunCollectionAsync()
    {
        var colIdx = _cmbCollection.SelectedIndex - 1;
        if (colIdx < 0)
        {
            MessageBox.Show("Please select a collection.", "No Collection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var col = _collections[colIdx];
        var iterations = (int)_numIterations.Value;
        var delay = (int)_numDelay.Value;
        var envIdx = _cmbEnvironment.SelectedIndex - 1;
        var env = envIdx >= 0 ? _environments[envIdx] : null;

        var allRequests = FlattenRequests(col.Items);
        if (allRequests.Count == 0)
        {
            MessageBox.Show("Collection has no requests.", "Empty Collection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _resultList.Items.Clear();
        _btnRun.Enabled = false;
        _btnStop.Enabled = true;
        _cts = new CancellationTokenSource();

        int total = allRequests.Count * iterations;
        int done = 0;
        int pass = 0, fail = 0;

        using var httpService = new HttpRequestService(_settings);
        var resolver = new VariableResolver();
        var scriptEngine = new ScriptEngine();
        var globals = _settings.GlobalVariables;

        try
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                foreach (var (item, itemCol) in allRequests)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    var req = item.Request!;
                    var resolved = resolver.ResolveRequest(req, env, col, globals);

                    // Pre-request script
                    if (!string.IsNullOrWhiteSpace(req.PreRequestScript))
                    {
                        var ctx = new ScriptContext { Request = resolved, Environment = env, Collection = col, Globals = globals };
                        scriptEngine.Execute(req.PreRequestScript, ctx);
                        resolved = resolver.ResolveRequest(req, env, col, globals);
                    }

                    ApiResponse resp;
                    try
                    {
                        resp = await httpService.SendAsync(resolved, _cts.Token);
                    }
                    catch
                    {
                        resp = new ApiResponse { ErrorMessage = "Request failed" };
                    }

                    // Test script
                    if (!string.IsNullOrWhiteSpace(req.TestScript))
                    {
                        var ctx = new ScriptContext { Request = resolved, Response = resp, Environment = env, Collection = col, Globals = globals };
                        scriptEngine.Execute(req.TestScript, ctx);
                        resp.TestResults.AddRange(ctx.TestResults);
                    }

                    done++;
                    int testPass = resp.TestResults.Count(t => t.Passed);
                    int testFail = resp.TestResults.Count(t => !t.Passed);
                    pass += testPass;
                    fail += testFail;

                    var statusCode = resp.ErrorMessage != null ? 0 : (int)resp.StatusCode;
                    var lvi = new ListViewItem((iter * allRequests.Count + done - (iterations - iter - 1) * allRequests.Count).ToString());
                    lvi.SubItems.Add(item.Name);
                    lvi.SubItems.Add(req.Method);
                    lvi.SubItems.Add(resp.ErrorMessage != null ? "Error" : statusCode.ToString());
                    lvi.SubItems.Add($"{resp.ResponseTimeMs}ms");
                    lvi.SubItems.Add(FormatBytes(resp.ResponseSizeBytes));
                    lvi.SubItems.Add(resp.TestResults.Count > 0 ? $"{testPass}/{resp.TestResults.Count}" : "-");
                    lvi.SubItems.Add(resp.ErrorMessage ?? "");

                    lvi.ForeColor = resp.ErrorMessage != null ? AppTheme.Error
                        : statusCode >= 200 && statusCode < 300 ? AppTheme.Success
                        : statusCode >= 400 ? AppTheme.Error
                        : AppTheme.Warning;
                    lvi.BackColor = AppTheme.Surface;

                    if (InvokeRequired) Invoke(() => _resultList.Items.Add(lvi));
                    else _resultList.Items.Add(lvi);

                    var pct = (int)(done * 100.0 / total);
                    if (InvokeRequired) Invoke(() => { _progress.Value = pct; _lblSummary.Text = $"Running... {done}/{total} | Pass: {pass} | Fail: {fail}"; });
                    else { _progress.Value = pct; _lblSummary.Text = $"Running... {done}/{total} | Pass: {pass} | Fail: {fail}"; }

                    if (delay > 0 && done < total)
                        await Task.Delay(delay, _cts.Token);
                }
                if (_cts.Token.IsCancellationRequested) break;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            if (InvokeRequired)
            {
                Invoke(() =>
                {
                    _btnRun.Enabled = true;
                    _btnStop.Enabled = false;
                    _progress.Value = 100;
                    _lblSummary.Text = $"Done — {done} requests | {pass} tests passed | {fail} tests failed";
                    _lblSummary.ForeColor = fail > 0 ? AppTheme.Error : AppTheme.Success;
                });
            }
            else
            {
                _btnRun.Enabled = true;
                _btnStop.Enabled = false;
                _progress.Value = 100;
                _lblSummary.Text = $"Done — {done} requests | {pass} tests passed | {fail} tests failed";
                _lblSummary.ForeColor = fail > 0 ? AppTheme.Error : AppTheme.Success;
            }
        }
    }

    private static List<(CollectionItem, DakiaCollection)> FlattenRequests(List<CollectionItem> items)
    {
        var result = new List<(CollectionItem, DakiaCollection)>();
        foreach (var item in items)
        {
            if (item.Type == "request" && item.Request != null)
                result.Add((item, null!));
            else if (item.Type == "folder")
                result.AddRange(FlattenRequests(item.Items));
        }
        return result;
    }

    private static void AddConfigRow(Panel parent, string label, Control ctrl, ref int y)
    {
        var lbl = new Label { Text = label, Location = new Point(12, y + 3), Width = 80, ForeColor = AppTheme.TextSecondary };
        ctrl.Location = new Point(100, y);
        ctrl.Height = 24;
        parent.Controls.Add(lbl);
        parent.Controls.Add(ctrl);
        y += 30;
    }

    private static string FormatBytes(long b)
    {
        if (b < 1024) return $"{b}B";
        if (b < 1024 * 1024) return $"{b / 1024.0:F1}KB";
        return $"{b / (1024.0 * 1024):F1}MB";
    }
}
