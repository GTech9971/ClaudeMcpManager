using Microsoft.Extensions.Logging;
using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager.Infrastructure;

/// <summary>
/// ディレクトリ管理の実装（ロギング対応）
/// </summary>
public class DirectoryService : IDirectoryService
{
    private readonly IMcpConfigService _configService;
    private readonly ILogger<DirectoryService>? _logger;

    // 固定のベースargs
    private static readonly string[] BaseArgs = { "-y", "@modelcontextprotocol/server-filesystem" };

    public DirectoryService(IMcpConfigService configService, ILogger<DirectoryService>? logger = null)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _logger = logger;
    }

    public async Task<CommandResult> AddDirectoryAsync(string directoryPath, bool force = false)
    {
        try
        {
            var fullPath = Path.GetFullPath(directoryPath);

            if (!Directory.Exists(fullPath))
            {
                return CommandResult.CreateError("指定されたディレクトリが存在しません。");
            }

            var config = await _configService.LoadConfigAsync();

            // filesystemサーバーの設定を取得または作成
            var filesystemServer = config.GetFilesystemServer();
            if (filesystemServer == null)
            {
                filesystemServer = new McpServer
                {
                    Command = "npx",
                    Args = BaseArgs.ToList()
                };
                config.SetFilesystemServer(filesystemServer);
            }

            // ベースargsを確保
            EnsureBaseArgs(filesystemServer);

            // 既存のディレクトリパスを取得
            var directoryPaths = GetDirectoryPaths(filesystemServer);

            // 既に存在するかチェック
            if (directoryPaths.Contains(fullPath))
            {
                if (!force)
                {
                    return CommandResult.CreateError("このディレクトリは既に設定されています。強制的に追加する場合は --force オプションを使用してください。");
                }
            }
            else
            {
                // 新しいディレクトリを追加
                directoryPaths.Add(fullPath);
                UpdateDirectoryPaths(filesystemServer, directoryPaths);
            }

            // 設定を保存
            await _configService.SaveConfigAsync(config);

            return CommandResult.CreateSuccess($"ディレクトリが正常に追加されました: {fullPath}");
        }
        catch (Exception ex)
        {
            return CommandResult.CreateError($"ディレクトリの追加に失敗しました: {ex.Message}", exception: ex);
        }
    }

    public async Task<CommandResult> RemoveDirectoryAsync(string directoryPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(directoryPath);
            var config = await _configService.LoadConfigAsync();

            if (!config.HasFilesystemServer())
            {
                return CommandResult.CreateError("filesystemサーバーが設定されていません。");
            }

            var filesystemServer = config.GetFilesystemServer()!;
            var directoryPaths = GetDirectoryPaths(filesystemServer);

            if (!directoryPaths.Contains(fullPath))
            {
                return CommandResult.CreateError("指定されたディレクトリは許可リストに存在しません。");
            }

            directoryPaths.Remove(fullPath);
            UpdateDirectoryPaths(filesystemServer, directoryPaths);
            config.SetFilesystemServer(filesystemServer);

            await _configService.SaveConfigAsync(config);

            return CommandResult.CreateSuccess($"ディレクトリが削除されました: {fullPath}");
        }
        catch (Exception ex)
        {
            return CommandResult.CreateError($"ディレクトリの削除に失敗しました: {ex.Message}", exception: ex);
        }
    }

    public async Task<CommandResult> RemoveDirectoryByIndexAsync(int index)
    {
        try
        {
            var config = await _configService.LoadConfigAsync();

            if (!config.HasFilesystemServer())
            {
                return CommandResult.CreateError("filesystemサーバーが設定されていません。");
            }

            var filesystemServer = config.GetFilesystemServer()!;
            var directoryPaths = GetDirectoryPaths(filesystemServer);

            var arrayIndex = index - 1; // 1-based to 0-based
            if (arrayIndex < 0 || arrayIndex >= directoryPaths.Count)
            {
                return CommandResult.CreateError("無効なインデックス番号です。");
            }

            var removedPath = directoryPaths[arrayIndex];
            directoryPaths.RemoveAt(arrayIndex);
            UpdateDirectoryPaths(filesystemServer, directoryPaths);

            config.SetFilesystemServer(filesystemServer);

            await _configService.SaveConfigAsync(config);

            return CommandResult.CreateSuccess($"ディレクトリが削除されました: {removedPath}");
        }
        catch (Exception ex)
        {
            return CommandResult.CreateError($"ディレクトリの削除に失敗しました: {ex.Message}", exception: ex);
        }
    }

    public async Task<IReadOnlyCollection<string>> GetDirectoriesAsync()
    {
        try
        {
            var config = await _configService.LoadConfigAsync();

            if (!config.HasFilesystemServer())
            {
                return Array.Empty<string>();
            }

            var filesystemServer = config.GetFilesystemServer()!;
            return GetDirectoryPaths(filesystemServer).ToList().AsReadOnly();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public async Task<IReadOnlyCollection<Services.DirectoryInfo>> GetDirectoryInfoAsync()
    {
        var directories = await GetDirectoriesAsync();
        ICollection<Services.DirectoryInfo> result = [];

        int index = 1;
        foreach (var dir in directories)
        {
            var exists = Directory.Exists(dir);
            DateTime? creationTime = null;

            if (exists)
            {
                try
                {
                    creationTime = new System.IO.DirectoryInfo(dir).CreationTime;
                }
                catch
                {
                    // 作成時間の取得に失敗してもエラーにしない
                }
            }

            result.Add(new Services.DirectoryInfo
            {
                Path = dir,
                Exists = exists,
                CreationTime = creationTime,
                Index = index++
            });
        }

        return result.ToList().AsReadOnly();
    }

    /// <summary>
    /// filesystemサーバーのargsにベースargs（npx, -y, @modelcontextprotocol/server-filesystem）が含まれていることを確認
    /// </summary>
    private static void EnsureBaseArgs(McpServer server)
    {
        // 必要なベースargsが不足している場合は追加
        for (int i = 0; i < BaseArgs.Length; i++)
        {
            if (server.Args.Count <= i || server.Args[i] != BaseArgs[i])
            {
                // 不足分を挿入
                server.Args.Insert(i, BaseArgs[i]);
            }
        }
    }

    /// <summary>
    /// ベースargs以降のディレクトリパスを取得
    /// </summary>
    private static List<string> GetDirectoryPaths(McpServer server)
    {
        var paths = new List<string>();

        // ベースargs以降の部分をディレクトリパスとして取得
        for (int i = BaseArgs.Length; i < server.Args.Count; i++)
        {
            paths.Add(server.Args[i]);
        }

        return paths;
    }

    /// <summary>
    /// ディレクトリパスをargsに設定（ベースargsは保持）
    /// </summary>
    private static void UpdateDirectoryPaths(McpServer server, IEnumerable<string> directoryPaths)
    {
        // ベースargs以降を削除
        while (server.Args.Count > BaseArgs.Length)
        {
            server.Args.RemoveAt(BaseArgs.Length);
        }

        // 新しいディレクトリパスを追加
        server.Args.AddRange(directoryPaths);
    }
}
