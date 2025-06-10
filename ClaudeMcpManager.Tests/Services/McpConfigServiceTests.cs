using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClaudeMcpManager.Infrastructure;
using ClaudeMcpManager.Models;
using Xunit;

namespace ClaudeMcpManager.Tests.Services;

/// <summary>
/// McpConfigServiceのテスト（DI対応）
/// 他のMCPサーバー設定が保持されることを確認
/// </summary>
public class McpConfigServiceTests : IDisposable
{
    private readonly string _testConfigPath;
    private readonly McpConfigService _service;
    private readonly IServiceProvider _serviceProvider;

    public McpConfigServiceTests()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");

        // DI設定
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _serviceProvider = services.BuildServiceProvider();

        var logger = _serviceProvider.GetService<ILogger<McpConfigService>>();
        _service = new McpConfigService(_testConfigPath, logger);
    }

    public void Dispose()
    {
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }
        // _serviceProvider.Dispose();
    }

    [Fact]
    public async Task LoadConfig_FileNotExists_CreatesNewConfig()
    {
        // Act
        var config = await _service.LoadConfigAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Empty(config.McpServers);
        Assert.True(_service.ConfigExists());
    }

    [Fact]
    public async Task SaveAndLoadConfig_PreservesAllMcpServers()
    {
        // Arrange
        var originalConfig = new McpConfig();

        // 他のMCPサーバーを追加
        originalConfig.McpServers["other-server"] = new McpServer
        {
            Command = "python",
            Args = new List<string> { "-m", "other.server" }
        };

        // filesystemサーバーを追加
        originalConfig.SetFilesystemServer(new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "/test/path" }
        });

        // Act
        await _service.SaveConfigAsync(originalConfig);
        var loadedConfig = await _service.LoadConfigAsync();

        // Assert
        Assert.Equal(2, loadedConfig.McpServers.Count);
        Assert.True(loadedConfig.HasFilesystemServer());
        Assert.Contains("other-server", loadedConfig.McpServers.Keys);

        var filesystemServer = loadedConfig.GetFilesystemServer();
        Assert.NotNull(filesystemServer);
        Assert.Equal("npx", filesystemServer.Command);
        Assert.Contains("/test/path", filesystemServer.Args);
    }

    [Fact]
    public async Task LoadConfig_WithUnknownProperties_PreservesExtensionData()
    {
        // Arrange
        var jsonWithUnknownProps = """
            {
              "mcpServers": {
                "filesystem": {
                  "command": "npx",
                  "args": ["-y", "@modelcontextprotocol/server-filesystem"]
                },
                "custom-server": {
                  "command": "python",
                  "args": ["-m", "custom.server"],
                  "customProperty": "customValue"
                }
              },
              "globalSetting": "someValue",
              "experimentalFeatures": {
                "feature1": true,
                "feature2": "enabled"
              }
            }
            """;

        await File.WriteAllTextAsync(_testConfigPath, jsonWithUnknownProps);

        // Act
        var config = await _service.LoadConfigAsync();

        // Assert
        Assert.Equal(2, config.McpServers.Count);
        Assert.NotNull(config.ExtensionData);
        Assert.True(config.ExtensionData.ContainsKey("globalSetting"));
        Assert.True(config.ExtensionData.ContainsKey("experimentalFeatures"));

        // Save and reload to ensure unknown properties are preserved
        await _service.SaveConfigAsync(config);
        var reloadedConfig = await _service.LoadConfigAsync();

        Assert.NotNull(reloadedConfig.ExtensionData);
        Assert.True(reloadedConfig.ExtensionData.ContainsKey("globalSetting"));
    }

    [Fact]
    public async Task CreateBackup_FileExists_CreatesBackupFile()
    {
        // Arrange
        var config = new McpConfig();
        await _service.SaveConfigAsync(config);

        // Act
        var backupPath = await _service.CreateBackupAsync();

        // Assert
        Assert.True(File.Exists(backupPath));
        Assert.Contains("backup_", backupPath);

        // Cleanup
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }
    }

    [Fact]
    public async Task RestoreFromBackup_ValidBackup_RestoresConfig()
    {
        // Arrange
        var originalConfig = new McpConfig();
        originalConfig.SetFilesystemServer(new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "/original/path" }
        });

        await _service.SaveConfigAsync(originalConfig);
        var backupPath = await _service.CreateBackupAsync();

        // Modify the config
        var modifiedConfig = new McpConfig();
        modifiedConfig.SetFilesystemServer(new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "/modified/path" }
        });
        await _service.SaveConfigAsync(modifiedConfig);

        // Act
        await _service.RestoreFromBackupAsync(backupPath);
        var restoredConfig = await _service.LoadConfigAsync();

        // Assert
        var filesystemServer = restoredConfig.GetFilesystemServer();
        Assert.NotNull(filesystemServer);
        Assert.Contains("/original/path", filesystemServer.Args);

        // Cleanup
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }
    }

    [Fact]
    public async Task LoadConfig_CorruptedJson_ThrowsInvalidOperationException()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigPath, "{ invalid json");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.LoadConfigAsync());

        Assert.Contains("設定ファイルの形式が正しくありません", exception.Message);
    }

    [Fact]
    public async Task LoadConfig_WithLogging_LogsAppropriateMessages()
    {
        // Arrange - この場合、実際のログ出力は手動で確認
        // テスト実行時にコンソール出力でログメッセージを確認できる

        // Act
        var config = await _service.LoadConfigAsync();

        // Assert
        Assert.NotNull(config);
        // ログ出力は実際のテスト実行時にコンソールで確認可能
    }
}
