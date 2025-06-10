using ClaudeMcpManager.Models;

namespace ClaudeMcpManager.Services;

/// <summary>
/// コンソール出力管理のインターフェース
/// </summary>
public interface IConsoleService
{
    /// <summary>
    /// 成功メッセージを出力
    /// </summary>
    void WriteSuccess(string message);

    /// <summary>
    /// エラーメッセージを出力
    /// </summary>
    void WriteError(string message);

    /// <summary>
    /// 警告メッセージを出力
    /// </summary>
    void WriteWarning(string message);

    /// <summary>
    /// 通常メッセージを出力
    /// </summary>
    void WriteInfo(string message);

    /// <summary>
    /// コマンド結果を適切な色で出力
    /// </summary>
    void WriteResult(CommandResult result);

    /// <summary>
    /// 色をリセット
    /// </summary>
    void ResetColor();
}
