using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager.Commands;

/// <summary>
/// listコマンドのハンドラー
/// </summary>
public class ListCommandHandler : ICommandHandler<ListOptions>
{
    private readonly IDirectoryService _directoryService;
    private readonly IMcpConfigService _configService;

    public ListCommandHandler(IDirectoryService directoryService, IMcpConfigService configService)
    {
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    public async Task<CommandResult> HandleAsync(ListOptions options)
    {
        try
        {
            var directories = await _directoryService.GetDirectoryInfoAsync();
            var message = await FormatDirectoryListAsync(directories, options.Verbose);

            return CommandResult.CreateSuccess(message);
        }
        catch (Exception ex)
        {
            return CommandResult.CreateError($"listコマンドの実行中にエラーが発生しました: {ex.Message}", exception: ex);
        }
    }

    private async Task<string> FormatDirectoryListAsync(IEnumerable<Services.DirectoryInfo> directories, bool verbose)
    {
        var output = new List<string>
            {
                "=== Claude Desktop MCP 許可ディレクトリ一覧 ===",
                ""
            };

        if (directories.Any() == false)
        {
            output.Add("許可されたディレクトリが設定されていません。");
            return string.Join("\n", output);
        }

        foreach (var dir in directories)
        {
            var line = $"{dir.Index,2}. ";

            if (dir.Exists)
            {
                line += "✓ ";
            }
            else
            {
                line += "✗ ";
            }

            line += dir.Path;

            if (!dir.Exists)
            {
                line += " (存在しません)";
            }

            if (verbose && dir.Exists && dir.CreationTime.HasValue)
            {
                line += $" (作成日: {dir.CreationTime.Value:yyyy-MM-dd})";
            }

            output.Add(line);
        }

        output.Add("");
        output.Add($"合計: {directories.Count()} ディレクトリ");

        if (verbose)
        {
            output.Add("");
            output.Add($"設定ファイル: {_configService.ConfigPath}");

            try
            {
                var config = await _configService.LoadConfigAsync();
                var filesystemServer = config.GetFilesystemServer();
                if (filesystemServer != null)
                {
                    output.Add($"コマンド: {filesystemServer.Command}");
                    output.Add($"引数: {string.Join(" ", filesystemServer.Args)}");
                }
            }
            catch
            {
                // 詳細情報の取得に失敗しても無視
            }
        }

        return string.Join("\n", output);
    }
}
