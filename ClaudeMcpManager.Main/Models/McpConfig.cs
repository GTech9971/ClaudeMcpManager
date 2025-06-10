using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeMcpManager.Models;

/// <summary>
/// Claude Desktop設定ファイルのルート構造
/// 未知のプロパティを保持して他のMCPサーバー設定を保護する
/// </summary>
public class McpConfig
{
    [JsonPropertyName("mcpServers")]
    public Dictionary<string, object> McpServers { get; set; } = new();

    /// <summary>
    /// 未知のプロパティを保持（他のMCPサーバー設定保護）
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    /// <summary>
    /// filesystemサーバーの設定を取得
    /// </summary>
    public McpServer? GetFilesystemServer()
    {
        if (!McpServers.ContainsKey("filesystem"))
            return null;

        var serverObj = McpServers["filesystem"];
        if (serverObj is JsonElement element)
        {
            return JsonSerializer.Deserialize<McpServer>(element.GetRawText());
        }
        
        return serverObj as McpServer;
    }

    /// <summary>
    /// filesystemサーバーの設定を設定
    /// </summary>
    public void SetFilesystemServer(McpServer server)
    {
        McpServers["filesystem"] = server;
    }

    /// <summary>
    /// filesystemサーバーが存在するかチェック
    /// </summary>
    public bool HasFilesystemServer()
    {
        return McpServers.ContainsKey("filesystem");
    }
}
