using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ClaudeMcpManager.Commands;
using ClaudeMcpManager.Infrastructure;
using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager;

/// <summary>
/// アプリケーションのエントリーポイント
/// Microsoft.Extensions.DependencyInjectionを使用したモジュール化アーキテクチャ
/// </summary>
public class Program
{
    private static IHost? _host;

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // ホストの初期化（DI、ロギング、設定管理）
            _host = ServiceProviderFactory.CreateHost();

            // コマンドライン引数の解析と処理
            var result = await Parser.Default.ParseArguments<AddOptions, ListOptions, RestartOptions, RemoveOptions>(args)
                .MapResult(
                    (AddOptions opts) => HandleCommandAsync(opts),
                    (ListOptions opts) => HandleCommandAsync(opts),
                    (RestartOptions opts) => HandleCommandAsync(opts),
                    (RemoveOptions opts) => HandleCommandAsync(opts),
                    HandleParseErrorAsync);

            return result;
        }
        catch (Exception ex)
        {
            var logger = _host?.Services.GetService<ILogger<Program>>();
            logger?.LogCritical(ex, "アプリケーションで予期しないエラーが発生しました");

            var console = _host?.Services.GetService<IConsoleService>();
            console?.WriteError($"予期しないエラーが発生しました: {ex.Message}");

#if DEBUG
            console?.WriteError($"スタックトレース: {ex.StackTrace}");
#endif

            return 1;
        }
        finally
        {
            _host?.Dispose();
        }
    }

    /// <summary>
    /// コマンドハンドラーを呼び出して処理を実行
    /// </summary>
    private static async Task<int> HandleCommandAsync<TOptions>(TOptions options)
    {
        try
        {
            var logger = _host!.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("コマンド実行開始: {CommandType}", typeof(TOptions).Name);

            var handler = _host.Services.GetRequiredService<ICommandHandler<TOptions>>();
            var console = _host.Services.GetRequiredService<IConsoleService>();

            var result = await handler.HandleAsync(options);
            console.WriteResult(result);

            logger.LogInformation("コマンド実行完了: {CommandType}, 結果: {Success}",
                typeof(TOptions).Name, result.Success);

            return result.ExitCode;
        }
        catch (Exception ex)
        {
            var logger = _host!.Services.GetService<ILogger<Program>>();
            logger?.LogError(ex, "コマンド実行中にエラーが発生しました: {CommandType}", typeof(TOptions).Name);

            var console = _host.Services.GetRequiredService<IConsoleService>();
            console.WriteError($"コマンド実行中にエラーが発生しました: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// コマンドライン引数の解析エラーを処理
    /// </summary>
    private static async Task<int> HandleParseErrorAsync(IEnumerable<CommandLine.Error> errors)
    {
        var logger = _host?.Services.GetService<ILogger<Program>>();
        var console = _host?.Services.GetService<IConsoleService>();

        // HelpRequestedエラーとVersionRequestedエラーは通常のヘルプ表示なので、エラーとして扱わない
        var errorList = errors.ToList();
        var hasHelpOrVersionRequest = errorList.Any(e =>
            e.Tag == CommandLine.ErrorType.HelpRequestedError ||
            e.Tag == CommandLine.ErrorType.VersionRequestedError);

        if (hasHelpOrVersionRequest)
        {
            logger?.LogInformation("ヘルプまたはバージョン情報が要求されました");
            return 0;
        }

        logger?.LogWarning("コマンドライン引数の解析に失敗しました");
        console?.WriteError("コマンドライン引数の解析に失敗しました。");

        foreach (var error in errorList)
        {
            logger?.LogWarning("引数エラー: {ErrorTag}", error.Tag);
            console?.WriteError($"エラー: {error.Tag}");
        }

        return 1;
    }
}
