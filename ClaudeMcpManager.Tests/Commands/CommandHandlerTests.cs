using ClaudeMcpManager.Commands;
using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;
using Moq;
using Xunit;

namespace ClaudeMcpManager.Tests.Commands;

/// <summary>
/// コマンドハンドラーのテスト
/// </summary>
public class CommandHandlerTests
{
    private readonly Mock<IDirectoryService> _mockDirectoryService;
    private readonly Mock<IMcpConfigService> _mockConfigService;
    private readonly Mock<IClaudeDesktopService> _mockClaudeDesktopService;

    public CommandHandlerTests()
    {
        _mockDirectoryService = new Mock<IDirectoryService>();
        _mockConfigService = new Mock<IMcpConfigService>();
        _mockClaudeDesktopService = new Mock<IClaudeDesktopService>();
    }

    [Fact]
    public async Task AddCommandHandler_ValidDirectory_ReturnsSuccessWithRestartMessage()
    {
        // Arrange
        var handler = new AddCommandHandler(_mockDirectoryService.Object);
        var options = new AddOptions { Directory = @"C:\test", Force = false };
        var serviceResult = CommandResult.CreateSuccess("ディレクトリが正常に追加されました");

        _mockDirectoryService
            .Setup(x => x.AddDirectoryAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await handler.HandleAsync(options);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("ディレクトリが正常に追加されました", result.Message);
        Assert.Contains("claude-mcp restart", result.Message);
    }

    [Fact]
    public async Task AddCommandHandler_ServiceError_ReturnsError()
    {
        // Arrange
        var handler = new AddCommandHandler(_mockDirectoryService.Object);
        var options = new AddOptions { Directory = @"C:\nonexistent" };
        var serviceResult = CommandResult.CreateError("ディレクトリが存在しません");

        _mockDirectoryService
            .Setup(x => x.AddDirectoryAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await handler.HandleAsync(options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("ディレクトリが存在しません", result.Message);
        Assert.DoesNotContain("claude-mcp restart", result.Message);
    }

    [Fact]
    public async Task ListCommandHandler_WithDirectories_ReturnsFormattedList()
    {
        // Arrange
        var handler = new ListCommandHandler(_mockDirectoryService.Object, _mockConfigService.Object);
        var options = new ListOptions { Verbose = false };
        var directoryInfos = new List<ClaudeMcpManager.Services.DirectoryInfo>
            {
                new() { Path = @"C:\test1", Exists = true, Index = 1 },
                new() { Path = @"C:\test2", Exists = false, Index = 2 }
            };

        _mockDirectoryService
            .Setup(x => x.GetDirectoryInfoAsync())
            .ReturnsAsync(directoryInfos);

        _mockConfigService
            .Setup(x => x.ConfigPath)
            .Returns(@"C:\config\path.json");

        // Act
        var result = await handler.HandleAsync(options);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("許可ディレクトリ一覧", result.Message);
        Assert.Contains(@"C:\test1", result.Message);
        Assert.Contains(@"C:\test2", result.Message);
        Assert.Contains("✓", result.Message); // 存在するディレクトリのマーク
        Assert.Contains("✗", result.Message); // 存在しないディレクトリのマーク
    }

    [Fact]
    public async Task ListCommandHandler_NoDirectories_ReturnsEmptyMessage()
    {
        // Arrange
        var handler = new ListCommandHandler(_mockDirectoryService.Object, _mockConfigService.Object);
        var options = new ListOptions { Verbose = false };

        _mockDirectoryService
            .Setup(x => x.GetDirectoryInfoAsync())
            .ReturnsAsync(new List<ClaudeMcpManager.Services.DirectoryInfo>());

        // Act
        var result = await handler.HandleAsync(options);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("設定されていません", result.Message);
    }

    [Fact]
    public async Task RemoveCommandHandler_ValidIndex_ReturnsSuccessWithRestartMessage()
    {
        // Arrange
        var handler = new RemoveCommandHandler(_mockDirectoryService.Object);
        var options = new RemoveOptions { Index = 1 };
        var serviceResult = CommandResult.CreateSuccess("ディレクトリが削除されました");

        _mockDirectoryService
            .Setup(x => x.RemoveDirectoryByIndexAsync(It.IsAny<int>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await handler.HandleAsync(options);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("ディレクトリが削除されました", result.Message);
        Assert.Contains("claude-mcp restart", result.Message);
    }

    [Fact]
    public async Task RemoveCommandHandler_ValidDirectory_ReturnsSuccessWithRestartMessage()
    {
        // Arrange
        var handler = new RemoveCommandHandler(_mockDirectoryService.Object);
        var options = new RemoveOptions { Directory = @"C:\test" };
        var serviceResult = CommandResult.CreateSuccess("ディレクトリが削除されました");

        _mockDirectoryService
            .Setup(x => x.RemoveDirectoryAsync(It.IsAny<string>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await handler.HandleAsync(options);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("ディレクトリが削除されました", result.Message);
        Assert.Contains("claude-mcp restart", result.Message);
    }

    [Fact]
    public async Task RestartCommandHandler_ValidOptions_CallsClaudeDesktopService()
    {
        // Arrange
        var handler = new RestartCommandHandler(_mockClaudeDesktopService.Object);
        var options = new RestartOptions { WaitTime = 1000 };
        var serviceResult = CommandResult.CreateSuccess("再起動が完了しました");

        _mockClaudeDesktopService
            .Setup(x => x.RestartAsync(It.IsAny<int>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await handler.HandleAsync(options);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("再起動が完了しました", result.Message);
        _mockClaudeDesktopService.Verify(x => x.RestartAsync(1000), Times.Once);
    }

    [Fact]
    public async Task AddCommandHandler_NullOptions_HandlesGracefully()
    {
        // Arrange
        var handler = new AddCommandHandler(_mockDirectoryService.Object);
        var options = new AddOptions(); // Directory is null
        var serviceResult = CommandResult.CreateSuccess("追加されました");

        _mockDirectoryService
            .Setup(x => x.AddDirectoryAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await handler.HandleAsync(options);

        // Assert
        Assert.True(result.Success);
        // 現在のディレクトリが使用されることを確認
        _mockDirectoryService.Verify(
            x => x.AddDirectoryAsync(
                It.Is<string>(path => !string.IsNullOrEmpty(path)),
                false),
            Times.Once);
    }
}
