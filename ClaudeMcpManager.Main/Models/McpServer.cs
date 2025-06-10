using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeMcpManager.Models;

/// <summary>
/// MCPサーバーの設定情報
/// </summary>
public class McpServer
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("args")]
    public List<string> Args { get; set; } = new();

    /// <summary>
    /// 未知のプロパティを保持（将来の拡張や他のサーバー設定保護）
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    /// <summary>
    /// 設定のディープコピーを作成
    /// </summary>
    public McpServer Clone()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<McpServer>(json) ?? new McpServer();
    }
}
