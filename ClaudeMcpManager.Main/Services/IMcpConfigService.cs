using ClaudeMcpManager.Models;

namespace ClaudeMcpManager.Services;

/// <summary>
/// MCP設定管理のインターフェース
/// </summary>
public interface IMcpConfigService
{
    /// <summary>
    /// 設定ファイルのパス
    /// </summary>
    string ConfigPath { get; }

    /// <summary>
    /// 設定ファイルが存在するかチェック
    /// </summary>
    bool ConfigExists();

    /// <summary>
    /// 設定を読み込み
    /// </summary>
    Task<McpConfig> LoadConfigAsync();

    /// <summary>
    /// 設定を保存
    /// </summary>
    Task SaveConfigAsync(McpConfig config);

    /// <summary>
    /// 設定ファイルのバックアップを作成
    /// </summary>
    Task<string> CreateBackupAsync();

    /// <summary>
    /// バックアップから設定を復元
    /// </summary>
    Task RestoreFromBackupAsync(string backupPath);
}
