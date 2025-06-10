using System.Text.Json;
using Microsoft.Extensions.Logging;
using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager.Infrastructure;

/// <summary>
/// MCP設定管理の実装（ロギング対応）
/// </summary>
public class McpConfigService : IMcpConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<McpConfigService>? _logger;

    public string ConfigPath { get; }

    public McpConfigService(string? configPath = null, ILogger<McpConfigService>? logger = null)
    {
        ConfigPath = configPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Claude", "claude_desktop_config.json");
        _logger = logger;
    }

    public bool ConfigExists()
    {
        return File.Exists(ConfigPath);
    }

    public async Task<McpConfig> LoadConfigAsync()
    {
        _logger?.LogInformation("設定ファイルを読み込み中: {ConfigPath}", ConfigPath);
        
        if (!ConfigExists())
        {
            _logger?.LogInformation("設定ファイルが見つかりません。新しい設定ファイルを作成します");
            var newConfig = new McpConfig();
            await SaveConfigAsync(newConfig);
            return newConfig;
        }

        try
        {
            var json = await File.ReadAllTextAsync(ConfigPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger?.LogWarning("設定ファイルが空です。新しい設定を作成します");
                return new McpConfig();
            }

            var config = JsonSerializer.Deserialize<McpConfig>(json, JsonOptions);
            _logger?.LogInformation("設定ファイルの読み込みが完了しました");
            return config ?? new McpConfig();
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "設定ファイルの形式が正しくありません");
            throw new InvalidOperationException($"設定ファイルの形式が正しくありません: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "設定ファイルの読み込みに失敗しました");
            throw new InvalidOperationException($"設定ファイルの読み込みに失敗しました: {ex.Message}", ex);
        }
    }

    public async Task SaveConfigAsync(McpConfig config)
    {
        _logger?.LogInformation("設定ファイルを保存中: {ConfigPath}", ConfigPath);
        
        try
        {
            var configDir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
            {
                _logger?.LogInformation("設定ディレクトリを作成: {ConfigDir}", configDir);
                Directory.CreateDirectory(configDir);
            }

            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(ConfigPath, json);
            _logger?.LogInformation("設定ファイルの保存が完了しました");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "設定ファイルの保存に失敗しました");
            throw new InvalidOperationException($"設定ファイルの保存に失敗しました: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateBackupAsync()
    {
        if (!ConfigExists())
        {
            _logger?.LogError("バックアップする設定ファイルが存在しません");
            throw new FileNotFoundException("バックアップする設定ファイルが存在しません。");
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = $"{ConfigPath}.backup_{timestamp}";
        
        _logger?.LogInformation("バックアップを作成中: {BackupPath}", backupPath);

        try
        {
            // .NET 8.0の非同期ファイルコピーを使用
            await Task.Run(() => File.Copy(ConfigPath, backupPath));

            _logger?.LogInformation("バックアップが正常に作成されました");
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "バックアップの作成に失敗しました");
            throw new InvalidOperationException($"バックアップの作成に失敗しました: {ex.Message}", ex);
        }
    }

    public async Task RestoreFromBackupAsync(string backupPath)
    {
        if (!File.Exists(backupPath))
        {
            _logger?.LogError("バックアップファイルが見つかりません: {BackupPath}", backupPath);
            throw new FileNotFoundException($"バックアップファイルが見つかりません: {backupPath}");
        }

        _logger?.LogInformation("バックアップから復元中: {BackupPath}", backupPath);

        try
        {
            // 現在の設定をテンポラリバックアップ
            var tempBackup = $"{ConfigPath}.temp_{DateTime.Now:yyyyMMdd_HHmmss}";
            if (ConfigExists())
            {
                _logger?.LogDebug("現在の設定をテンポラリバックアップ: {TempBackup}", tempBackup);
                await Task.Run(() => File.Copy(ConfigPath, tempBackup));
            }

            // バックアップから復元
            await Task.Run(() => File.Copy(backupPath, ConfigPath, true));

            // 復元が成功したらテンポラリバックアップを削除
            if (File.Exists(tempBackup))
            {
                File.Delete(tempBackup);
            }

            _logger?.LogInformation("バックアップからの復元が完了しました");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "バックアップからの復元に失敗しました");
            throw new InvalidOperationException($"バックアップからの復元に失敗しました: {ex.Message}", ex);
        }
    }
}
