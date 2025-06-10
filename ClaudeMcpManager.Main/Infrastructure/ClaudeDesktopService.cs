using System.Diagnostics;
using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager.Infrastructure;

/// <summary>
/// Claude Desktop管理の実装
/// </summary>
public class ClaudeDesktopService : IClaudeDesktopService
{
    private static readonly string[] ProcessNames = { "Claude Desktop", "Claude" };

    public bool IsRunning()
    {
        return ProcessNames.Any(name => Process.GetProcessesByName(name).Length > 0);
    }

    public async Task<CommandResult> StopAsync(int timeoutMs = 5000)
    {
        try
        {
            var processes = new List<Process>();

            foreach (var processName in ProcessNames)
            {
                processes.AddRange(Process.GetProcessesByName(processName));
            }

            if (processes.Count == 0)
            {
                return CommandResult.CreateSuccess("Claude Desktopプロセスは実行されていません。");
            }

            ICollection<int> stoppedProcesses = [];

            foreach (var process in processes)
            {
                try
                {
                    var processId = process.Id;
                    process.Kill();

                    // プロセス終了を待機
                    if (!process.WaitForExit(timeoutMs))
                    {
                        return CommandResult.CreateError($"プロセス (PID: {processId}) の終了がタイムアウトしました。");
                    }

                    stoppedProcesses.Add(processId);
                }
                catch (Exception ex)
                {
                    return CommandResult.CreateError($"プロセス終了時にエラーが発生しました: {ex.Message}", exception: ex);
                }
                finally
                {
                    process.Dispose();
                }
            }

            return CommandResult.CreateSuccess($"Claude Desktopプロセスを停止しました (PID: {string.Join(", ", stoppedProcesses)})");
        }
        catch (Exception ex)
        {
            return CommandResult.CreateError($"プロセス停止中にエラーが発生しました: {ex.Message}", exception: ex);
        }
    }

    public async Task<CommandResult> StartAsync()
    {
        try
        {
            var claudePath = FindClaudeDesktopPath();

            if (string.IsNullOrEmpty(claudePath))
            {
                return CommandResult.CreateError("Claude Desktopのパスが見つかりません。手動で起動してください。");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = claudePath,
                UseShellExecute = true
            };

            Process.Start(startInfo);

            // 起動確認のため少し待機
            await Task.Delay(1000);

            if (IsRunning())
            {
                return CommandResult.CreateSuccess("Claude Desktopが正常に起動されました。");
            }
            else
            {
                return CommandResult.CreateError("Claude Desktopの起動を確認できませんでした。");
            }
        }
        catch (Exception ex)
        {
            return CommandResult.CreateError($"Claude Desktop起動中にエラーが発生しました: {ex.Message}", exception: ex);
        }
    }

    public async Task<CommandResult> RestartAsync(int waitTimeMs = 2000)
    {
        try
        {
            // 停止
            var stopResult = await StopAsync();
            if (!stopResult.Success)
            {
                return stopResult;
            }

            // 待機
            if (waitTimeMs > 0)
            {
                await Task.Delay(waitTimeMs);
            }

            // 開始
            var startResult = await StartAsync();
            if (!startResult.Success)
            {
                return startResult;
            }

            return CommandResult.CreateSuccess("Claude Desktopの再起動が完了しました。");
        }
        catch (Exception ex)
        {
            return CommandResult.CreateError($"再起動中にエラーが発生しました: {ex.Message}", exception: ex);
        }
    }

    public string? FindClaudeDesktopPath()
    {
        // 一般的なClaude Desktopのインストール場所を検索
        var possiblePaths = new[]
        {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                           "Programs", "Claude", "Claude Desktop.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                           "Claude Desktop", "Claude Desktop.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                           "Claude Desktop", "Claude Desktop.exe")
            };

        return possiblePaths.FirstOrDefault(File.Exists);
    }
}
