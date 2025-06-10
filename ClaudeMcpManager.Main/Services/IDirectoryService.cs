using ClaudeMcpManager.Models;

namespace ClaudeMcpManager.Services;

/// <summary>
/// ディレクトリ管理のインターフェース
/// </summary>
public interface IDirectoryService
{
    /// <summary>
    /// ディレクトリを追加
    /// </summary>
    Task<CommandResult> AddDirectoryAsync(string directoryPath, bool force = false);

    /// <summary>
    /// ディレクトリを削除
    /// </summary>
    Task<CommandResult> RemoveDirectoryAsync(string directoryPath);

    /// <summary>
    /// インデックスでディレクトリを削除
    /// </summary>
    Task<CommandResult> RemoveDirectoryByIndexAsync(int index);

    /// <summary>
    /// 許可されたディレクトリ一覧を取得
    /// </summary>
    Task<IReadOnlyCollection<string>> GetDirectoriesAsync();

    /// <summary>
    /// ディレクトリの詳細情報を取得
    /// </summary>
    Task<IReadOnlyCollection<DirectoryInfo>> GetDirectoryInfoAsync();
}

/// <summary>
/// ディレクトリ情報
/// </summary>
public class DirectoryInfo
{
    public string Path { get; set; } = "";
    public bool Exists { get; set; }
    public DateTime? CreationTime { get; set; }
    public int Index { get; set; }
}
