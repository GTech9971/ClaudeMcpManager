using ClaudeMcpManager.Infrastructure;
using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;
using Moq;
using Xunit;

namespace ClaudeMcpManager.Tests.Services;

/// <summary>
/// DirectoryServiceのテスト
/// </summary>
public class DirectoryServiceTests : IDisposable
{
    private readonly Mock<IMcpConfigService> _mockConfigService;
    private readonly DirectoryService _service;
    private readonly string _testDirectory;

    public DirectoryServiceTests()
    {
        _mockConfigService = new Mock<IMcpConfigService>();
        _service = new DirectoryService(_mockConfigService.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task AddDirectory_NewDirectory_AddsSuccessfully()
    {
        // Arrange
        var config = new McpConfig();
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var result = await _service.AddDirectoryAsync(_testDirectory);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("正常に追加されました", result.Message);
        _mockConfigService.Verify(x => x.SaveConfigAsync(It.IsAny<McpConfig>()), Times.Once);
    }

    [Fact]
    public async Task AddDirectory_ExistingDirectory_WithoutForce_ReturnsError()
    {
        // Arrange
        var config = new McpConfig();
        var filesystemServer = new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", _testDirectory }
        };
        config.SetFilesystemServer(filesystemServer);
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var result = await _service.AddDirectoryAsync(_testDirectory, force: false);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("既に設定されています", result.Message);
    }

    [Fact]
    public async Task AddDirectory_ExistingDirectory_WithForce_Succeeds()
    {
        // Arrange
        var config = new McpConfig();
        var filesystemServer = new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", _testDirectory }
        };
        config.SetFilesystemServer(filesystemServer);
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var result = await _service.AddDirectoryAsync(_testDirectory, force: true);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("正常に追加されました", result.Message);
    }

    [Fact]
    public async Task AddDirectory_NonExistentDirectory_ReturnsError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non_existent_directory");
        var config = new McpConfig();
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var result = await _service.AddDirectoryAsync(nonExistentPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("存在しません", result.Message);
    }

    [Fact]
    public async Task RemoveDirectory_ExistingDirectory_RemovesSuccessfully()
    {
        // Arrange
        var config = new McpConfig();
        var filesystemServer = new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", _testDirectory }
        };
        config.SetFilesystemServer(filesystemServer);
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var result = await _service.RemoveDirectoryAsync(_testDirectory);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("削除されました", result.Message);
        _mockConfigService.Verify(x => x.SaveConfigAsync(It.IsAny<McpConfig>()), Times.Once);
    }

    [Fact]
    public async Task RemoveDirectory_NonExistentDirectory_ReturnsError()
    {
        // Arrange
        var config = new McpConfig();
        var filesystemServer = new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem" }
        };
        config.SetFilesystemServer(filesystemServer);
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var result = await _service.RemoveDirectoryAsync(_testDirectory);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("許可リストに存在しません", result.Message);
    }

    [Fact]
    public async Task RemoveDirectoryByIndex_ValidIndex_RemovesSuccessfully()
    {
        // Arrange
        var config = new McpConfig();
        var filesystemServer = new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", _testDirectory, "/another/path" }
        };
        config.SetFilesystemServer(filesystemServer);
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var result = await _service.RemoveDirectoryByIndexAsync(1); // 1-based index

        // Assert
        Assert.True(result.Success);
        Assert.Contains(_testDirectory, result.Message);
        _mockConfigService.Verify(x => x.SaveConfigAsync(It.IsAny<McpConfig>()), Times.Once);
    }

    [Fact]
    public async Task RemoveDirectoryByIndex_InvalidIndex_ReturnsError()
    {
        // Arrange
        var config = new McpConfig();
        var filesystemServer = new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", _testDirectory }
        };
        config.SetFilesystemServer(filesystemServer);
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var result = await _service.RemoveDirectoryByIndexAsync(5); // Out of range

        // Assert
        Assert.False(result.Success);
        Assert.Contains("無効なインデックス番号", result.Message);
    }

    [Fact]
    public async Task GetDirectories_WithFilesystemServer_ReturnsDirectories()
    {
        // Arrange
        var testPaths = new List<string> { _testDirectory, "/another/path" };
        var config = new McpConfig();
        var filesystemServer = new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem" }
        };
        filesystemServer.Args.AddRange(testPaths);
        config.SetFilesystemServer(filesystemServer);
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var directories = await _service.GetDirectoriesAsync();

        // Assert
        Assert.Equal(2, directories.Count);
        Assert.Contains(_testDirectory, directories);
        Assert.Contains("/another/path", directories);
    }

    [Fact]
    public async Task GetDirectories_NoFilesystemServer_ReturnsEmpty()
    {
        // Arrange
        var config = new McpConfig();
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var directories = await _service.GetDirectoriesAsync();

        // Assert
        Assert.Empty(directories);
    }

    [Fact]
    public async Task GetDirectoryInfo_MixedExistenceDirectories_ReturnsCorrectInfo()
    {
        // Arrange
        var nonExistentPath = "/non/existent/path";
        var config = new McpConfig();
        var filesystemServer = new McpServer
        {
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", _testDirectory, nonExistentPath }
        };
        config.SetFilesystemServer(filesystemServer);
        _mockConfigService.Setup(x => x.LoadConfigAsync()).ReturnsAsync(config);

        // Act
        var directoryInfos = await _service.GetDirectoryInfoAsync();

        // Assert
        Assert.Equal(2, directoryInfos.Count);

        var existingDirInfo = directoryInfos.First(d => d.Path == _testDirectory);
        Assert.True(existingDirInfo.Exists);
        Assert.NotNull(existingDirInfo.CreationTime);
        Assert.Equal(1, existingDirInfo.Index);

        var nonExistentDirInfo = directoryInfos.First(d => d.Path == nonExistentPath);
        Assert.False(nonExistentDirInfo.Exists);
        Assert.Null(nonExistentDirInfo.CreationTime);
        Assert.Equal(2, nonExistentDirInfo.Index);
    }
}
