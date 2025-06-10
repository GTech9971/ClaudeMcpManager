# ClaudeMcpManager.Tests

このディレクトリには、Claude MCP Managerの包括的なテストスイートが含まれています。

## テスト構造

```
Tests/
├── ClaudeMcpManager.Tests.csproj  # テストプロジェクトファイル
├── Services/                      # サービス層のテスト
│   ├── McpConfigServiceTests.cs   # 設定管理のテスト（重大バグ修正検証）
│   ├── DirectoryServiceTests.cs   # ディレクトリ管理のテスト
│   └── ClaudeDesktopServiceTests.cs # Claude Desktop操作のテスト
├── Commands/                      # コマンドハンドラーのテスト
│   └── CommandHandlerTests.cs     # 各コマンドの動作テスト
└── Integration/                   # 統合テスト
    └── IntegrationTests.cs        # エンドツーエンドテスト
```

## テスト実行方法

### 全テスト実行
```bash
cd C:\Users\user\Documents\GitHub\ClaudeMcpManager
dotnet test Tests/ClaudeMcpManager.Tests.csproj
```

### 特定のテストカテゴリーのみ実行
```bash
# サービス層のテストのみ
dotnet test Tests/ClaudeMcpManager.Tests.csproj --filter "FullyQualifiedName~Services"

# コマンドハンドラーのテストのみ
dotnet test Tests/ClaudeMcpManager.Tests.csproj --filter "FullyQualifiedName~Commands"

# 統合テストのみ
dotnet test Tests/ClaudeMcpManager.Tests.csproj --filter "FullyQualifiedName~Integration"
```

### テストカバレッジ付き実行
```bash
dotnet test Tests/ClaudeMcpManager.Tests.csproj --collect:"XPlat Code Coverage"
```

## 主要テストケース

### 🔒 重大バグ修正の検証テスト

#### McpConfigServiceTests
- **他MCPサーバー設定保護**: `SaveAndLoadConfig_PreservesAllMcpServers()`
- **未知プロパティ保持**: `LoadConfig_WithUnknownProperties_PreservesExtensionData()`
- **バックアップ/復元**: `RestoreFromBackup_ValidBackup_RestoresConfig()`
- **エラーハンドリング**: `LoadConfig_CorruptedJson_ThrowsInvalidOperationException()`

#### DirectoryServiceTests
- **ディレクトリ追加**: `AddDirectory_NewDirectory_AddsSuccessfully()`
- **重複チェック**: `AddDirectory_ExistingDirectory_WithoutForce_ReturnsError()`
- **強制追加**: `AddDirectory_ExistingDirectory_WithForce_Succeeds()`
- **削除操作**: `RemoveDirectory_ExistingDirectory_RemovesSuccessfully()`
- **インデックス削除**: `RemoveDirectoryByIndex_ValidIndex_RemovesSuccessfully()`

### 🧪 統合テスト

#### IntegrationTests
- **完全ワークフロー**: `FullWorkflow_AddListRemove_WorksCorrectly()`
- **設定永続化**: `ConfigPersistence_MaintainsOtherMcpServers()`
- **バックアップ/復元**: `BackupAndRestore_PreservesCompleteConfiguration()`
- **ファイルシステム反映**: `DirectoryInfo_ReflectsActualFileSystem()`
- **並行処理**: `ConcurrentOperations_HandleGracefully()`
- **エラー回復**: `ErrorRecovery_HandlesCorruptedConfig()`

## テスト環境要件

- .NET 8.0 SDK
- xUnit テストフレームワーク
- Moq モックライブラリ
- 一時ディレクトリへの読み書き権限

## 重要な検証項目

### ✅ 他MCPサーバー設定保護の確認
テストは以下を保証します：
1. **JsonExtensionData**による未知プロパティの完全保持
2. **filesystem以外のMCPサーバー設定**の意図しない削除防止
3. **設定ファイルの完全性**維持
4. **バックアップ/復元機能**の正確性

### ⚡ パフォーマンステスト
- 大量ディレクトリ操作の処理時間
- 並行アクセス時の安全性
- メモリ使用量の妥当性

### 🔄 リグレッションテスト
- 既存コマンドの完全互換性
- 設定ファイル形式の後方互換性
- エラーメッセージの一貫性

## CI/CD統合

GitHub Actionsでの自動テスト実行設定例：

```yaml
- name: Run Tests
  run: dotnet test Tests/ClaudeMcpManager.Tests.csproj --logger trx --collect:"XPlat Code Coverage"

- name: Upload Test Results
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: TestResults/
```

## トラブルシューティング

### よくある問題

1. **権限エラー**: 一時ディレクトリへの書き込み権限を確認
2. **ファイルロック**: Claude Desktopが実行中の場合はテストが失敗する可能性
3. **パス区切り文字**: Windows/Linux/macOSでのパス表記の違い

### デバッグ実行
```bash
dotnet test Tests/ClaudeMcpManager.Tests.csproj --logger "console;verbosity=detailed"
```
