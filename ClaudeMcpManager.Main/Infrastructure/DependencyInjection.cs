using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ClaudeMcpManager.Commands;
using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager.Infrastructure;

/// <summary>
/// Microsoft.Extensions.DependencyInjectionを使用したサービス登録
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Claude MCP Managerのサービスを登録
    /// </summary>
    public static IServiceCollection AddClaudeMcpManagerServices(
        this IServiceCollection services,
        string? configPath = null)
    {
        // インフラストラクチャサービス
        services.AddSingleton<IMcpConfigService>(provider =>
            new McpConfigService(configPath));
        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddSingleton<IClaudeDesktopService, ClaudeDesktopService>();

        // ビジネスロジックサービス
        services.AddScoped<IDirectoryService, DirectoryService>();

        // コマンドハンドラー
        services.AddScoped<ICommandHandler<AddOptions>, AddCommandHandler>();
        services.AddScoped<ICommandHandler<ListOptions>, ListCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveOptions>, RemoveCommandHandler>();
        services.AddScoped<ICommandHandler<RestartOptions>, RestartCommandHandler>();

        // ロギング設定
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        return services;
    }
}

/// <summary>
/// サービスプロバイダーファクトリ
/// </summary>
public static class ServiceProviderFactory
{
    /// <summary>
    /// 設定済みのサービスプロバイダーを作成
    /// </summary>
    public static IServiceProvider CreateServiceProvider(string? configPath = null)
    {
        var services = new ServiceCollection();
        services.AddClaudeMcpManagerServices(configPath);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// ホストビルダーを使用したサービスプロバイダーを作成（より本格的な設定）
    /// </summary>
    public static IHost CreateHost(string? configPath = null)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddClaudeMcpManagerServices(configPath);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();
    }
}
