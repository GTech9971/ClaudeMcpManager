# Claude MCP Manager

Claude Desktop MCPのファイルシステムディレクトリ管理を簡単にするコマンドラインツールです。

## 機能

- **ディレクトリ追加**: 現在のディレクトリまたは指定したディレクトリを許可リストに追加
- **ディレクトリ一覧**: 許可されたディレクトリの一覧表示（存在確認付き）
- **ディレクトリ削除**: 不要なディレクトリを許可リストから削除
- **自動再起動**: Claude Desktopの自動再起動で設定を即座に適用

## インストール

### グローバルツールとしてインストール

```bash
dotnet tool install --global ClaudeMcpManager
```

### ローカルビルド

```bash
git clone <repository-url>
cd claude-mcp-manager
dotnet build
```

## 使用方法

### 基本コマンド

```bash
# 現在のディレクトリを許可リストに追加
claude-mcp add

# 特定のディレクトリを追加
claude-mcp add -d "C:\MyProject"

# 強制的に追加（既存の場合も）
claude-mcp add --force

# 許可されたディレクトリの一覧表示
claude-mcp list

# 詳細情報付きで一覧表示
claude-mcp list --verbose

# ディレクトリを削除（現在のディレクトリ）
claude-mcp remove

# 特定のディレクトリを削除
claude-mcp remove -d "C:\MyProject"

# インデックス番号で削除
claude-mcp remove -i 1

# Claude Desktopを再起動
claude-mcp restart

# カスタム待機時間で再起動
claude-mcp restart --wait 5000

# ヘルプの表示
claude-mcp --help
```

### オプション

#### add コマンド

- `-d, --directory`: 追加するディレクトリのパス
- `-f, --force`: 既存の場合でも強制的に追加

#### list コマンド

- `-v, --verbose`: 詳細情報を表示

#### remove コマンド

- `-d, --directory`: 削除するディレクトリのパス
- `-i, --index`: 削除するディレクトリのインデックス番号

#### restart コマンド

- `-w, --wait`: 再起動前の待機時間（ミリ秒、デフォルト: 2000）

## 設定ファイル

設定は以下の場所に保存されます：

```text
%APPDATA%\Claude\claude_desktop_config.json
```

## 要件

- .NET 8.0 以上
- Windows, macOS, または Linux

## ライセンス

MIT License

## 貢献

プルリクエストや課題報告は歓迎します。

## サポート

問題や質問がある場合は、GitHubのIssuesページをご利用ください。
