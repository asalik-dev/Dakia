namespace Dakia.Models;

public class WorkspaceSettings
{
    public string ActiveEnvironmentId { get; set; } = "";
    public bool SslVerification { get; set; } = true;
    public bool FollowRedirects { get; set; } = true;
    public int TimeoutMs { get; set; } = 30000;
    public string ProxyHost { get; set; } = "";
    public int ProxyPort { get; set; } = 8080;
    public bool UseProxy { get; set; } = false;
    public string ProxyUsername { get; set; } = "";
    public string ProxyPassword { get; set; } = "";
    public string Theme { get; set; } = "dark";
    public int MaxHistoryItems { get; set; } = 200;
    public int FontSize { get; set; } = 13;
    public string EditorFont { get; set; } = "Consolas";
    public bool AutoSaveRequests { get; set; } = true;
    public bool SendCookies { get; set; } = true;
    public bool StoreCookies { get; set; } = true;
    public bool EnableScripts { get; set; } = true;
    public bool TrimRequestBody { get; set; } = false;
    public string DefaultProtocol { get; set; } = "https";
    public bool EncodeQueryParams { get; set; } = true;
    public bool ShowRequestHeaders { get; set; } = false;
    public bool DisableBodyPrettify { get; set; } = false;
    public int SidebarWidth { get; set; } = 280;
    public int ResponsePanelHeight { get; set; } = 300;
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 800;
    public bool WindowMaximized { get; set; } = false;
    public Dictionary<string, string> GlobalVariables { get; set; } = [];
}
