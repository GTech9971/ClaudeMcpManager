using CommandLine;

namespace ClaudeMcpManager.Models;
// Command Line Options
[Verb("add", HelpText = "現在のディレクトリを許可リストに追加します")]
public class AddOptions
{
    [Option('d', "directory", Required = false, HelpText = "追加するディレクトリのパス（指定しない場合は現在のディレクトリ）")]
    public string? Directory { get; set; }

    [Option('f', "force", Required = false, Default = false, HelpText = "既に存在する場合でも強制的に追加")]
    public bool Force { get; set; }
}

[Verb("list", HelpText = "許可されたディレクトリの一覧を表示します")]
public class ListOptions
{
    [Option('v', "verbose", Required = false, Default = false, HelpText = "詳細情報を表示")]
    public bool Verbose { get; set; }
}

[Verb("restart", HelpText = "Claude Desktopアプリケーションを再起動します")]
public class RestartOptions
{
    [Option('w', "wait", Required = false, Default = 2000, HelpText = "再起動前の待機時間（ミリ秒）")]
    public int WaitTime { get; set; }
}

[Verb("remove", HelpText = "指定されたディレクトリを許可リストから削除します")]
public class RemoveOptions
{
    [Option('d', "directory", Required = false, HelpText = "削除するディレクトリのパス（指定しない場合は現在のディレクトリ）")]
    public string? Directory { get; set; }

    [Option('i', "index", Required = false, HelpText = "削除するディレクトリのインデックス番号")]
    public int? Index { get; set; }
}

[Verb("debug", HelpText = "設定ファイルのデバッグ情報を表示します")]
public class DebugOptions
{
}
