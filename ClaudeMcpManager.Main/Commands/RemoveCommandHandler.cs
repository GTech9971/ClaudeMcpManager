using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager.Commands;

/// <summary>
/// removeコマンドのハンドラー
/// </summary>
public class RemoveCommandHandler : ICommandHandler<RemoveOptions>
{
    private readonly IDirectoryService _directoryService;

    public RemoveCommandHandler(IDirectoryService directoryService)
    {
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
    }

    public async Task<CommandResult> HandleAsync(RemoveOptions options)
    {
        try
        {
            CommandResult result;

            if (options.Index.HasValue)
            {
                result = await _directoryService.RemoveDirectoryByIndexAsync(options.Index.Value);
            }
            else
            {
                var targetDir = options.Directory ?? Directory.GetCurrentDirectory();
                result = await _directoryService.RemoveDirectoryAsync(targetDir);
            }

            if (result.Success)
            {
                // 削除成功時に再起動メッセージを付加
                result.Message += "\n変更を適用するには 'claude-mcp restart' を実行してください。";
            }

            return result;
        }
        catch (Exception ex)
        {
            return CommandResult.CreateError($"removeコマンドの実行中にエラーが発生しました: {ex.Message}", exception: ex);
        }
    }
}
