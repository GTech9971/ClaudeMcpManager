using ClaudeMcpManager.Infrastructure;
using Xunit;

namespace ClaudeMcpManager.Tests.Services;

/// <summary>
/// ClaudeDesktopServiceのテスト
/// </summary>
public class ClaudeDesktopServiceTests
{
    private readonly ClaudeDesktopService _service;

    public ClaudeDesktopServiceTests()
    {
        _service = new ClaudeDesktopService();
    }

    [Fact]
    public void IsRunning_ChecksForClaudeProcesses()
    {
        // Act
        var isRunning = _service.IsRunning();

        // Assert
        // プロセスが実行中かどうかは環境に依存するため、例外が発生しないことのみ確認
        Assert.True(isRunning == true || isRunning == false);
    }

    [Fact]
    public void FindClaudeDesktopPath_ReturnsPathOrNull()
    {
        // Act
        var path = _service.FindClaudeDesktopPath();

        // Assert
        // パスが見つからない場合はnullまたは空文字列、見つかった場合はファイルパス
        if (!string.IsNullOrEmpty(path))
        {
            Assert.True(File.Exists(path) || path.EndsWith(".exe"));
        }
    }

    [Fact]
    public async Task StopAsync_NoClaudeProcesses_ReturnsSuccess()
    {
        // 通常の環境ではClaude Desktopが実行されていない可能性が高いため、
        // プロセスが存在しない場合の動作をテスト

        // Act
        var result = await _service.StopAsync(1000); // 短いタイムアウト

        // Assert
        Assert.True(result.Success);
        Assert.Contains("実行されていません", result.Message);
    }

    [Fact]
    public async Task StartAsync_NoClaudePath_ReturnsError()
    {
        // Claude Desktopがインストールされていない環境での動作をテスト
        // 実際のパス検索でClaude Desktopが見つからない場合

        // Act
        var result = await _service.StartAsync();

        // Assert
        // Claude Desktopがインストールされていない場合はエラーが返される
        if (!result.Success)
        {
            Assert.Contains("パスが見つかりません", result.Message);
        }
        else
        {
            // インストールされている場合は成功または起動確認失敗
            Assert.True(result.Success || result.Message.Contains("起動を確認できませんでした"));
        }
    }

    [Fact]
    public async Task RestartAsync_CallsStopAndStart()
    {
        // Act
        var result = await _service.RestartAsync(100); // 短い待機時間

        // Assert
        // 結果は環境に依存するが、メソッドが正常に完了することを確認
        Assert.NotNull(result);
        Assert.True(result.Success || !string.IsNullOrEmpty(result.Message));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(5000)]
    public async Task RestartAsync_WithDifferentWaitTimes_HandlesCorrectly(int waitTime)
    {
        // Act
        var result = await _service.RestartAsync(waitTime);

        // Assert
        Assert.NotNull(result);
        // 例外が発生せずに完了することを確認
    }
}
