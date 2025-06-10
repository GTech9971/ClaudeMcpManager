using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager.Commands;

/// <summary>
/// restartコマンドのハンドラー
/// </summary>
public class RestartCommandHandler : ICommandHandler<RestartOptions>
{
    private readonly IClaudeDesktopService _claudeDesktopService;

    public RestartCommandHandler(IClaudeDesktopService claudeDesktopService)
    {
        _claudeDesktopService = claudeDesktopService ?? throw new ArgumentNullException(nameof(claudeDesktopService));
    }

    public async Task<CommandResult> HandleAsync(RestartOptions options)
    {
        try
        {
            return await _claudeDesktopService.RestartAsync(options.WaitTime);
        }
        catch (Exception ex)
        {
            return CommandResult.CreateError($"restartコマンドの実行中にエラーが発生しました: {ex.Message}", exception: ex);
        }
    }
}
