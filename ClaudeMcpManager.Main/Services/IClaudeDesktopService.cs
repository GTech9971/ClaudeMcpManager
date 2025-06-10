using ClaudeMcpManager.Models;

namespace ClaudeMcpManager.Services;

/// <summary>
/// Claude Desktop再起動管理のインターフェース
/// </summary>
public interface IClaudeDesktopService
{
    /// <summary>
    /// Claude Desktopが実行中かチェック
    /// </summary>
    bool IsRunning();

    /// <summary>
    /// Claude Desktopプロセスを停止
    /// </summary>
    Task<CommandResult> StopAsync(int timeoutMs = 5000);

    /// <summary>
    /// Claude Desktopを起動
    /// </summary>
    Task<CommandResult> StartAsync();

    /// <summary>
    /// Claude Desktopを再起動
    /// </summary>
    Task<CommandResult> RestartAsync(int waitTimeMs = 2000);

    /// <summary>
    /// Claude Desktopのインストールパスを検索
    /// </summary>
    string? FindClaudeDesktopPath();
}

