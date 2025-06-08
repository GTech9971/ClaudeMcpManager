using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;

namespace ClaudeMcpManager
{
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

    // JSON Classes
    public class McpConfig
    {
        [JsonPropertyName("mcpServers")]
        public Dictionary<string, McpServer> McpServers { get; set; } = new();
    }

    public class McpServer
    {
        [JsonPropertyName("command")]
        public string Command { get; set; } = "";

        [JsonPropertyName("args")]
        public List<string> Args { get; set; } = new();
    }

    public class Program
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Claude", "claude_desktop_config.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // 固定のコマンドとベースargs
        private static readonly string[] BaseArgs = { "-y", "@modelcontextprotocol/server-filesystem" };

        public static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<AddOptions, ListOptions, RestartOptions, RemoveOptions>(args)
                .MapResult(
                    (AddOptions opts) => HandleAdd(opts),
                    (ListOptions opts) => HandleList(opts),
                    (RestartOptions opts) => HandleRestart(opts),
                    (RemoveOptions opts) => HandleRemove(opts),
                    errs => HandleParseError(errs));
        }

        private static int HandleParseError(IEnumerable<CommandLine.Error> errors)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("コマンドライン引数の解析に失敗しました。");
            Console.ResetColor();

            // エラーの詳細を表示
            foreach (var error in errors)
            {
                Console.WriteLine($"エラー: {error.Tag}");
            }

            return 1;
        }

        private static int HandleAdd(AddOptions options)
        {
            try
            {
                string targetDir = Path.GetFullPath(options.Directory ?? Directory.GetCurrentDirectory());

                Console.WriteLine($"ディレクトリを追加: {targetDir}");

                if (!Directory.Exists(targetDir))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("指定されたディレクトリが存在しません。");
                    Console.ResetColor();
                    return 1;
                }

                var config = LoadConfig();

                // filesystemサーバーの設定を取得または作成
                if (!config.McpServers.ContainsKey("filesystem"))
                {
                    config.McpServers["filesystem"] = new McpServer
                    {
                        Command = "npx",
                        Args = new List<string>(BaseArgs)
                    };
                }

                var filesystemServer = config.McpServers["filesystem"];

                // ベースargsを確保
                EnsureBaseArgs(filesystemServer);

                // 既存のディレクトリパスを取得（ベースargs以降）
                var directoryPaths = GetDirectoryPaths(filesystemServer);

                // 既に存在するかチェック
                if (directoryPaths.Contains(targetDir))
                {
                    if (!options.Force)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("このディレクトリは既に設定されています。");
                        Console.WriteLine("強制的に追加する場合は --force オプションを使用してください。");
                        Console.ResetColor();
                        return 0;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("このディレクトリは既に設定されていますが、強制的に処理を続行します。");
                        Console.ResetColor();
                    }
                }
                else
                {
                    // 新しいディレクトリを追加
                    directoryPaths.Add(targetDir);
                    UpdateDirectoryPaths(filesystemServer, directoryPaths);
                }

                // 設定を保存
                SaveConfig(config);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ディレクトリが正常に追加されました。");
                Console.WriteLine("変更を適用するには 'claude-mcp restart' を実行してください。");
                Console.ResetColor();

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"エラーが発生しました: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static int HandleList(ListOptions options)
        {
            try
            {
                var config = LoadConfig();

                Console.WriteLine("=== Claude Desktop MCP 許可ディレクトリ一覧 ===");
                Console.WriteLine();

                if (!config.McpServers.ContainsKey("filesystem"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("filesystemサーバーが設定されていません。");
                    Console.ResetColor();
                    return 0;
                }

                var filesystemServer = config.McpServers["filesystem"];
                var directoryPaths = GetDirectoryPaths(filesystemServer);

                if (directoryPaths.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("許可されたディレクトリが設定されていません。");
                    Console.ResetColor();
                    return 0;
                }

                for (int i = 0; i < directoryPaths.Count; i++)
                {
                    var dir = directoryPaths[i];
                    var exists = Directory.Exists(dir);

                    Console.Write($"{i + 1,2}. ");

                    if (exists)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("✓ ");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("✗ ");
                    }

                    Console.ResetColor();
                    Console.Write(dir);

                    if (!exists)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write(" (存在しません)");
                        Console.ResetColor();
                    }

                    if (options.Verbose && exists)
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(dir);
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write($" (作成日: {dirInfo.CreationTime:yyyy-MM-dd})");
                            Console.ResetColor();
                        }
                        catch { }
                    }

                    Console.WriteLine();
                }

                Console.WriteLine();
                Console.WriteLine($"合計: {directoryPaths.Count} ディレクトリ");

                if (options.Verbose)
                {
                    Console.WriteLine();
                    Console.WriteLine("設定ファイル: " + ConfigPath);
                    Console.WriteLine($"コマンド: {filesystemServer.Command}");
                    Console.WriteLine($"引数: {string.Join(" ", filesystemServer.Args)}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"エラーが発生しました: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static int HandleRestart(RestartOptions options)
        {
            try
            {
                Console.WriteLine("Claude Desktopアプリケーションを再起動しています...");

                // Claude Desktopプロセスを終了
                var claudeProcesses = Process.GetProcessesByName("Claude Desktop");
                if (claudeProcesses.Length == 0)
                {
                    claudeProcesses = Process.GetProcessesByName("Claude");
                }

                foreach (var process in claudeProcesses)
                {
                    try
                    {
                        Console.WriteLine($"Claude Desktop プロセス (PID: {process.Id}) を終了しています...");
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"プロセス終了時にエラー: {ex.Message}");
                    }
                }

                // 指定された時間待機
                if (options.WaitTime > 0)
                {
                    Console.WriteLine($"{options.WaitTime}ms 待機中...");
                    System.Threading.Thread.Sleep(options.WaitTime);
                }

                // Claude Desktopを再起動
                string claudePath = FindClaudeDesktopPath();

                if (!string.IsNullOrEmpty(claudePath))
                {
                    Console.WriteLine("Claude Desktopを起動しています...");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = claudePath,
                        UseShellExecute = true
                    });

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Claude Desktopの再起動が完了しました。");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Claude Desktopのパスが見つかりません。手動で起動してください。");
                    Console.ResetColor();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"再起動中にエラーが発生しました: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static int HandleRemove(RemoveOptions options)
        {
            try
            {
                var config = LoadConfig();

                if (!config.McpServers.ContainsKey("filesystem"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("filesystemサーバーが設定されていません。");
                    Console.ResetColor();
                    return 0;
                }

                var filesystemServer = config.McpServers["filesystem"];
                var directoryPaths = GetDirectoryPaths(filesystemServer);

                if (directoryPaths.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("許可されたディレクトリが設定されていません。");
                    Console.ResetColor();
                    return 0;
                }

                string targetDir = "";

                if (options.Index.HasValue)
                {
                    int index = options.Index.Value - 1;
                    if (index < 0 || index >= directoryPaths.Count)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("無効なインデックス番号です。");
                        Console.ResetColor();
                        return 1;
                    }
                    targetDir = directoryPaths[index];
                }
                else
                {
                    targetDir = Path.GetFullPath(options.Directory ?? Directory.GetCurrentDirectory());
                }

                if (!directoryPaths.Contains(targetDir))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("指定されたディレクトリは許可リストに存在しません。");
                    Console.ResetColor();
                    return 0;
                }

                directoryPaths.Remove(targetDir);
                UpdateDirectoryPaths(filesystemServer, directoryPaths);

                SaveConfig(config);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"ディレクトリが削除されました: {targetDir}");
                Console.WriteLine("変更を適用するには 'claude-mcp restart' を実行してください。");
                Console.ResetColor();

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"エラーが発生しました: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        /// <summary>
        /// filesystemサーバーのargsにベースargs（npx, -y, @modelcontextprotocol/server-filesystem）が含まれていることを確認
        /// </summary>
        private static void EnsureBaseArgs(McpServer server)
        {
            // 必要なベースargsが不足している場合は追加
            for (int i = 0; i < BaseArgs.Length; i++)
            {
                if (server.Args.Count <= i || server.Args[i] != BaseArgs[i])
                {
                    // 不足分を挿入
                    server.Args.Insert(i, BaseArgs[i]);
                }
            }
        }

        /// <summary>
        /// ベースargs以降のディレクトリパスを取得
        /// </summary>
        private static List<string> GetDirectoryPaths(McpServer server)
        {
            var paths = new List<string>();

            // ベースargs以降の部分をディレクトリパスとして取得
            for (int i = BaseArgs.Length; i < server.Args.Count; i++)
            {
                paths.Add(server.Args[i]);
            }

            return paths;
        }

        /// <summary>
        /// ディレクトリパスをargsに設定（ベースargsは保持）
        /// </summary>
        private static void UpdateDirectoryPaths(McpServer server, List<string> directoryPaths)
        {
            // ベースargs以降を削除
            while (server.Args.Count > BaseArgs.Length)
            {
                server.Args.RemoveAt(BaseArgs.Length);
            }

            // 新しいディレクトリパスを追加
            server.Args.AddRange(directoryPaths);
        }

        private static string FindClaudeDesktopPath()
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

            return possiblePaths.FirstOrDefault(File.Exists) ?? "";
        }

        private static McpConfig LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                Console.WriteLine($"設定ファイルが見つかりません。新しい設定ファイルを作成します: {ConfigPath}");
                var newConfig = new McpConfig();
                SaveConfig(newConfig);
                return newConfig;
            }

            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<McpConfig>(json, JsonOptions) ?? new McpConfig();
        }

        private static void SaveConfig(McpConfig config)
        {
            var configDir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir!);
            }

            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
    }
}