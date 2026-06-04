namespace Dakia.Theme;

public static class AppTheme
{
    // ── Color Palette ────────────────────────────────────────────────────────
    public static Color Background      = Color.FromArgb(0x1E, 0x22, 0x28);
    public static Color Surface         = Color.FromArgb(0x25, 0x29, 0x30);
    public static Color SurfaceElevated = Color.FromArgb(0x2C, 0x31, 0x39);
    public static Color Border          = Color.FromArgb(0x36, 0x3B, 0x44);
    public static Color BorderLight     = Color.FromArgb(0x45, 0x4D, 0x5A);
    public static Color TextPrimary     = Color.FromArgb(0xE8, 0xE8, 0xE8);
    public static Color TextSecondary   = Color.FromArgb(0xAA, 0xB2, 0xBF);
    public static Color TextDim         = Color.FromArgb(0x6B, 0x74, 0x7F);
    public static Color Accent          = Color.FromArgb(0xFF, 0x6C, 0x37);
    public static Color AccentHover     = Color.FromArgb(0xFF, 0x85, 0x55);
    public static Color AccentDim       = Color.FromArgb(0x80, 0x36, 0x1B);
    public static Color Success         = Color.FromArgb(0x49, 0xCC, 0x90);
    public static Color Warning         = Color.FromArgb(0xFC, 0xA1, 0x30);
    public static Color Error           = Color.FromArgb(0xF9, 0x3E, 0x3E);
    public static Color Info            = Color.FromArgb(0x61, 0xAF, 0xFE);
    public static Color SidebarBg      = Color.FromArgb(0x1A, 0x1E, 0x24);
    public static Color TabActiveBg    = Color.FromArgb(0x2C, 0x31, 0x39);
    public static Color SelectionBg    = Color.FromArgb(0x26, 0x4F, 0x78);
    public static Color HoverBg        = Color.FromArgb(0x2E, 0x34, 0x3D);

    // ── Method Colors ────────────────────────────────────────────────────────
    public static Color MethodGet       = Color.FromArgb(0x61, 0xAF, 0xFE);
    public static Color MethodPost      = Color.FromArgb(0x49, 0xCC, 0x90);
    public static Color MethodPut       = Color.FromArgb(0xFC, 0xA1, 0x30);
    public static Color MethodDelete    = Color.FromArgb(0xF9, 0x3E, 0x3E);
    public static Color MethodPatch     = Color.FromArgb(0x50, 0xE3, 0xC2);
    public static Color MethodHead      = Color.FromArgb(0x90, 0x12, 0xFE);
    public static Color MethodOptions   = Color.FromArgb(0x0D, 0x5A, 0xA7);

    // ── Syntax Highlight Colors ───────────────────────────────────────────────
    public static Color SyntaxKey      = Color.FromArgb(0xF8, 0x8D, 0x7A);
    public static Color SyntaxString   = Color.FromArgb(0xC3, 0xE8, 0x8D);
    public static Color SyntaxNumber   = Color.FromArgb(0xF7, 0x8C, 0x6C);
    public static Color SyntaxBool     = Color.FromArgb(0xC7, 0x92, 0xEA);
    public static Color SyntaxNull     = Color.FromArgb(0xC7, 0x92, 0xEA);
    public static Color SyntaxBrace    = Color.FromArgb(0x89, 0xDD, 0xFF);
    public static Color SyntaxComment  = Color.FromArgb(0x67, 0x6E, 0x79);

    // ── Fonts ─────────────────────────────────────────────────────────────────
    public static Font FontNormal      = new Font("Segoe UI", 9f, FontStyle.Regular);
    public static Font FontSmall       = new Font("Segoe UI", 8f, FontStyle.Regular);
    public static Font FontBold        = new Font("Segoe UI", 9f, FontStyle.Bold);
    public static Font FontMono        = new Font("Consolas", 10f, FontStyle.Regular);
    public static Font FontMonoSmall   = new Font("Consolas", 9f, FontStyle.Regular);
    public static Font FontLarge       = new Font("Segoe UI", 11f, FontStyle.Regular);
    public static Font FontMethod      = new Font("Segoe UI", 9f, FontStyle.Bold);

    // ── Status Code Colors ────────────────────────────────────────────────────
    public static Color StatusColor(int code) => code switch
    {
        >= 200 and < 300 => Success,
        >= 300 and < 400 => Warning,
        >= 400 and < 500 => Error,
        >= 500 => Color.FromArgb(0xFF, 0x00, 0x7F),
        _ => TextDim
    };

    // ── Method Color ───────────────────────────────────────────────────────────
    public static Color MethodColor(string method) => method.ToUpperInvariant() switch
    {
        "GET"     => MethodGet,
        "POST"    => MethodPost,
        "PUT"     => MethodPut,
        "DELETE"  => MethodDelete,
        "PATCH"   => MethodPatch,
        "HEAD"    => MethodHead,
        "OPTIONS" => MethodOptions,
        _         => TextSecondary
    };

    // ── Apply to control trees ─────────────────────────────────────────────────
    public static void ApplyDark(Control root)
    {
        root.BackColor = Background;
        root.ForeColor = TextPrimary;
        foreach (Control c in root.Controls)
            ApplyToControl(c);
    }

    private static void ApplyToControl(Control c)
    {
        switch (c)
        {
            case MenuStrip ms:
                ms.BackColor = Surface;
                ms.ForeColor = TextPrimary;
                ms.RenderMode = ToolStripRenderMode.Professional;
                ms.Renderer = new DarkToolStripRenderer();
                ApplyMenuItems(ms.Items);
                break;
            case StatusStrip ss:
                ss.BackColor = Surface;
                ss.ForeColor = TextSecondary;
                ss.Renderer = new DarkToolStripRenderer();
                break;
            case ToolStrip ts:
                ts.BackColor = Surface;
                ts.ForeColor = TextPrimary;
                ts.Renderer = new DarkToolStripRenderer();
                break;
            case TabControl tc:
                tc.BackColor = Background;
                tc.ForeColor = TextPrimary;
                break;
            case DataGridView dg:
                dg.BackgroundColor = Surface;
                dg.ForeColor = TextPrimary;
                dg.GridColor = Border;
                dg.DefaultCellStyle.BackColor = Surface;
                dg.DefaultCellStyle.ForeColor = TextPrimary;
                dg.DefaultCellStyle.SelectionBackColor = SelectionBg;
                dg.DefaultCellStyle.SelectionForeColor = TextPrimary;
                dg.ColumnHeadersDefaultCellStyle.BackColor = SurfaceElevated;
                dg.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
                dg.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceElevated;
                dg.EnableHeadersVisualStyles = false;
                dg.RowHeadersDefaultCellStyle.BackColor = Surface;
                dg.RowHeadersDefaultCellStyle.SelectionBackColor = SelectionBg;
                dg.AlternatingRowsDefaultCellStyle.BackColor = SurfaceElevated;
                dg.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
                break;
            case TreeView tv:
                tv.BackColor = SidebarBg;
                tv.ForeColor = TextPrimary;
                tv.LineColor = Border;
                break;
            case ListBox lb:
                lb.BackColor = Surface;
                lb.ForeColor = TextPrimary;
                break;
            case ComboBox cb:
                cb.BackColor = SurfaceElevated;
                cb.ForeColor = TextPrimary;
                cb.FlatStyle = FlatStyle.Flat;
                break;
            case TextBox tb:
                tb.BackColor = SurfaceElevated;
                tb.ForeColor = TextPrimary;
                tb.BorderStyle = BorderStyle.FixedSingle;
                break;
            case RichTextBox rtb:
                rtb.BackColor = SurfaceElevated;
                rtb.ForeColor = TextPrimary;
                break;
            case Button btn:
                btn.BackColor = SurfaceElevated;
                btn.ForeColor = TextPrimary;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = Border;
                break;
            case CheckBox chk:
                chk.BackColor = Color.Transparent;
                chk.ForeColor = TextPrimary;
                break;
            case RadioButton rb:
                rb.BackColor = Color.Transparent;
                rb.ForeColor = TextPrimary;
                break;
            case Label lbl:
                lbl.BackColor = Color.Transparent;
                lbl.ForeColor = TextPrimary;
                break;
            case Panel pnl:
                pnl.BackColor = Background;
                break;
            case SplitContainer sc:
                sc.BackColor = Border;
                sc.Panel1.BackColor = Background;
                sc.Panel2.BackColor = Background;
                break;
            case GroupBox gb:
                gb.BackColor = Color.Transparent;
                gb.ForeColor = TextSecondary;
                break;
            default:
                c.BackColor = Background;
                c.ForeColor = TextPrimary;
                break;
        }

        foreach (Control child in c.Controls)
            ApplyToControl(child);
    }

    private static void ApplyMenuItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.BackColor = Surface;
            item.ForeColor = TextPrimary;
            if (item is ToolStripMenuItem mi)
                ApplyMenuItems(mi.DropDownItems);
        }
    }
}

internal class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public DarkToolStripRenderer() : base(new DarkColorTable()) { }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        // suppress default border
        using var pen = new Pen(AppTheme.Border);
        var r = e.AffectedBounds;
        e.Graphics.DrawLine(pen, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected || e.Item.Pressed)
        {
            using var b = new SolidBrush(AppTheme.HoverBg);
            e.Graphics.FillRectangle(b, e.Item.ContentRectangle);
        }
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(AppTheme.Border);
        var r = e.Item.ContentRectangle;
        e.Graphics.DrawLine(pen, r.Left + 4, r.Height / 2, r.Right - 4, r.Height / 2);
    }

    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected || e.Item.Pressed)
        {
            using var b = new SolidBrush(AppTheme.HoverBg);
            e.Graphics.FillRectangle(b, new Rectangle(0, 0, e.Item.Width, e.Item.Height));
        }
    }
}

internal class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => AppTheme.Border;
    public override Color MenuItemBorder => AppTheme.Border;
    public override Color MenuItemSelected => AppTheme.HoverBg;
    public override Color MenuItemSelectedGradientBegin => AppTheme.HoverBg;
    public override Color MenuItemSelectedGradientEnd => AppTheme.HoverBg;
    public override Color MenuItemPressedGradientBegin => AppTheme.SelectionBg;
    public override Color MenuItemPressedGradientEnd => AppTheme.SelectionBg;
    public override Color MenuStripGradientBegin => AppTheme.Surface;
    public override Color MenuStripGradientEnd => AppTheme.Surface;
    public override Color ToolStripDropDownBackground => AppTheme.Surface;
    public override Color ImageMarginGradientBegin => AppTheme.Surface;
    public override Color ImageMarginGradientMiddle => AppTheme.Surface;
    public override Color ImageMarginGradientEnd => AppTheme.Surface;
    public override Color SeparatorDark => AppTheme.Border;
    public override Color SeparatorLight => AppTheme.Border;
    public override Color ToolStripBorder => AppTheme.Border;
    public override Color ToolStripContentPanelGradientBegin => AppTheme.Surface;
    public override Color ToolStripContentPanelGradientEnd => AppTheme.Surface;
    public override Color ToolStripGradientBegin => AppTheme.Surface;
    public override Color ToolStripGradientEnd => AppTheme.Surface;
    public override Color ToolStripGradientMiddle => AppTheme.Surface;
    public override Color ButtonCheckedGradientBegin => AppTheme.AccentDim;
    public override Color ButtonCheckedGradientEnd => AppTheme.AccentDim;
    public override Color ButtonSelectedGradientBegin => AppTheme.HoverBg;
    public override Color ButtonSelectedGradientEnd => AppTheme.HoverBg;
    public override Color ButtonPressedGradientBegin => AppTheme.SelectionBg;
    public override Color ButtonPressedGradientEnd => AppTheme.SelectionBg;
    public override Color CheckBackground => AppTheme.AccentDim;
    public override Color CheckPressedBackground => AppTheme.Accent;
    public override Color CheckSelectedBackground => AppTheme.AccentDim;
}
