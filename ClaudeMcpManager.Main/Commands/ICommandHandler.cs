using ClaudeMcpManager.Models;

namespace ClaudeMcpManager.Commands;

/// <summary>
/// コマンドハンドラーのベースインターフェース
/// </summary>
public interface ICommandHandler<TOptions>
{
    Task<CommandResult> HandleAsync(TOptions options);
}
