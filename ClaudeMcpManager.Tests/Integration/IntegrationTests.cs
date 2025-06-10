using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClaudeMcpManager.Infrastructure;
using ClaudeMcpManager.Models;
using Xunit;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager.Tests.Integration;

/// <summary>
/// 統合テスト - Microsoft.Extensions.DependencyInjectionを使用したエンドツーエンドテスト
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly string _testConfigPath;
    private readonly string _testDirectory1;
    private readonly string _testDirectory2;
    private readonly IServiceProvider _serviceProvider;
    private readonly McpConfigService _configService;
    private readonly DirectoryService _directoryService;

    public IntegrationTests()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"integration_test_config_{Guid.NewGuid()}.json");
        _testDirectory1 = Path.Combine(Path.GetTempPath(), $"test_dir_1_{Guid.NewGuid()}");
        _testDirectory2 = Path.Combine(Path.GetTempPath(), $"test_dir_2_{Guid.NewGuid()}");

        Directory.CreateDirectory(_testDirectory1);
        Directory.CreateDirectory(_testDirectory2);

        // DI設定
        var services = new ServiceCollection();
        services.AddClaudeMcpManagerServices(_testConfigPath);
        _serviceProvider = services.BuildServiceProvider();

        _configService = (McpConfigService)_serviceProvider.GetRequiredService<IMcpConfigService>();
        _directoryService = (DirectoryService)_serviceProvider.GetRequiredService<IDirectoryService>();
    }

    public void Dispose()
    {
        if (File.Exists(_testConfigPath))
            File.Delete(_testConfigPath);

        if (Directory.Exists(_testDirectory1))
            Directory.Delete(_testDirectory1, true);

        if (Directory.Exists(_testDirectory2))
            Directory.Delete(_testDirectory2, true);

        // _serviceProvider.Dispose();
    }

    [Fact]
    public async Task FullWorkflow_AddListRemove_WorksCorrectlyWithDI()
    {
        // 1. Add first directory
        var addResult1 = await _directoryService.AddDirectoryAsync(_testDirectory1);
        Assert.True(addResult1.Success);

        // 2. Verify it was added
        var directories = await _directoryService.GetDirectoriesAsync();
        Assert.Single(directories);
        Assert.Contains(_testDirectory1, directories);

        // 3. Add second directory
        var addResult2 = await _directoryService.AddDirectoryAsync(_testDirectory2);
        Assert.True(addResult2.Success);

        // 4. Verify both directories exist
        directories = await _directoryService.GetDirectoriesAsync();
        Assert.Equal(2, directories.Count);
        Assert.Contains(_testDirectory1, directories);
        Assert.Contains(_testDirectory2, directories);

        // 5. Remove first directory
        var removeResult = await _directoryService.RemoveDirectoryAsync(_testDirectory1);
        Assert.True(removeResult.Success);

        // 6. Verify only second directory remains
        directories = await _directoryService.GetDirectoriesAsync();
        Assert.Single(directories);
        Assert.Contains(_testDirectory2, directories);
        Assert.DoesNotContain(_testDirectory1, directories);

        // 7. Remove by index
        var removeByIndexResult = await _directoryService.RemoveDirectoryByIndexAsync(1);
        Assert.True(removeByIndexResult.Success);

        // 8. Verify no directories remain
        directories = await _directoryService.GetDirectoriesAsync();
        Assert.Empty(directories);
    }

    [Fact]
    public async Task ConfigPersistence_MaintainsOtherMcpServersWithDI()
    {
        // 1. Create config with other MCP server
        var config = new McpConfig();
        config.McpServers["other-server"] = new McpServer
        {
            Command = "python",
            Args = new List<string> { "-m", "other.server", "--port", "8080" }
        };

        // Add some extension data
        config.ExtensionData = new Dictionary<string, JsonElement>
        {
            ["globalSetting"] = JsonSerializer.SerializeToElement("testValue"),
            ["experimentalFeatures"] = JsonSerializer.SerializeToElement(new { enabled = true, beta = false })
        };

        await _configService.SaveConfigAsync(config);

        // 2. Add filesystem directory using DirectoryService
        var addResult = await _directoryService.AddDirectoryAsync(_testDirectory1);
        Assert.True(addResult.Success);

        // 3. Reload config and verify everything is preserved
        var reloadedConfig = await _configService.LoadConfigAsync();

        // Filesystem server should exist
        Assert.True(reloadedConfig.HasFilesystemServer());
        var filesystemServer = reloadedConfig.GetFilesystemServer();
        Assert.NotNull(filesystemServer);
        Assert.Contains(_testDirectory1, filesystemServer.Args);

        // Other server should be preserved
        Assert.Contains("other-server", reloadedConfig.McpServers.Keys);

        // Extension data should be preserved
        Assert.NotNull(reloadedConfig.ExtensionData);
        Assert.True(reloadedConfig.ExtensionData.ContainsKey("globalSetting"));
        Assert.True(reloadedConfig.ExtensionData.ContainsKey("experimentalFeatures"));
    }

    [Fact]
    public async Task DependencyInjection_ServicesResolveCorrectly()
    {
        // Verify that all services can be resolved from the DI container
        var configService = _serviceProvider.GetRequiredService<IMcpConfigService>();
        var directoryService = _serviceProvider.GetRequiredService<IDirectoryService>();
        var consoleService = _serviceProvider.GetRequiredService<IConsoleService>();
        var claudeDesktopService = _serviceProvider.GetRequiredService<IClaudeDesktopService>();

        // Verify services are not null
        Assert.NotNull(configService);
        Assert.NotNull(directoryService);
        Assert.NotNull(consoleService);
        Assert.NotNull(claudeDesktopService);

        // Verify services are properly configured
        Assert.Equal(_testConfigPath, configService.ConfigPath);
    }

    [Fact]
    public async Task Logging_IntegrationWithServices()
    {
        // Test that services with logging work correctly
        var logger = _serviceProvider.GetService<ILogger<DirectoryService>>();
        Assert.NotNull(logger);

        // Perform an operation that should generate logs
        var result = await _directoryService.AddDirectoryAsync(_testDirectory1);
        Assert.True(result.Success);

        // Verify operation completed successfully (logs would be visible in test output)
        var directories = await _directoryService.GetDirectoriesAsync();
        Assert.Single(directories);
    }

    [Fact]
    public async Task ServiceLifetime_ScopedServicesWorkCorrectly()
    {
        // Test that scoped services work correctly
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();

        var directoryService1 = scope1.ServiceProvider.GetRequiredService<IDirectoryService>();
        var directoryService2 = scope2.ServiceProvider.GetRequiredService<IDirectoryService>();

        // Different scopes should have different instances for scoped services
        Assert.NotSame(directoryService1, directoryService2);

        // But they should work with the same underlying data
        await directoryService1.AddDirectoryAsync(_testDirectory1);
        var directories1 = await directoryService1.GetDirectoriesAsync();
        var directories2 = await directoryService2.GetDirectoriesAsync();

        Assert.Single(directories1);
        Assert.Single(directories2);
        Assert.Equal(directories1, directories2);
    }
}
