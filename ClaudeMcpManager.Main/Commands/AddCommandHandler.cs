using Microsoft.Extensions.Logging;
using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager.Commands;

/// <summary>
/// addコマンドのハンドラー（ロギング対応）
/// </summary>
public class AddCommandHandler : ICommandHandler<AddOptions>
{
    private readonly IDirectoryService _directoryService;
    private readonly ILogger<AddCommandHandler>? _logger;

    public AddCommandHandler(IDirectoryService directoryService, ILogger<AddCommandHandler>? logger = null)
    {
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
        _logger = logger;
    }

    public async Task<CommandResult> HandleAsync(AddOptions options)
    {
        try
        {
            var targetDir = options.Directory ?? Directory.GetCurrentDirectory();
            _logger?.LogInformation("ディレクトリ追加処理開始: {Directory}, Force: {Force}", targetDir, options.Force);

            var result = await _directoryService.AddDirectoryAsync(targetDir, options.Force);

            if (result.Success)
            {
                // 追加で再起動メッセージを付加
                result.Message += "\n変更を適用するには 'claude-mcp restart' を実行してください。";
                _logger?.LogInformation("ディレクトリ追加が正常に完了しました: {Directory}", targetDir);
            }
            else
            {
                _logger?.LogWarning("ディレクトリ追加に失敗しました: {Directory}, 理由: {Message}", targetDir, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "addコマンドの実行中にエラーが発生しました");
            return CommandResult.CreateError($"addコマンドの実行中にエラーが発生しました: {ex.Message}", exception: ex);
        }
    }
}
