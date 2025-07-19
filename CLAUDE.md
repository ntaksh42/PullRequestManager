# Pull Request Manager - Development Session Summary

## プロジェクト概要
C# WPF アプリケーション「Pull Request Manager」（旧 Azure DevOps Tool）の開発セッション記録

## 主要な実装内容

### 1. 基本機能
- Azure DevOps API を使用したプルリクエスト取得・表示
- WPF UI フレームワークでの Grid 表示
- CLI インターフェースとの分離アーキテクチャ（DLL レベル分離）

### 2. 実装済み機能
#### UI 機能
- 右クリックメニューでの列表示/非表示切り替え
- クリック可能なタイトルリンク（ブラウザで PR ページを開く）
- ハイパーリンク風の見た目とホバー効果
- コミッター・タイトルでの検索機能
- 日付フィルタリング（指定日以降のPR取得）
- 作成者（Author）フィルタリング
- ターゲットブランチフィルタリング
- グリッドでの編集ファイルリスト表示

#### 設定機能
- アプリ終了時の設定ファイル保存・復元
- PAT（Personal Access Token）の暗号化保存（Windows DPAPI使用）

#### 高度な機能（全実装済み）
- **PR詳細ペイン**: 包括的なPR情報表示（説明、ブランチ、ステータス、レビュワー、変更サマリー、関連ワークアイテム）
- **リッチステータス情報**: アイコン付きステータス表示、ビルド状況、承認状況
- **クイックアクション**: 右クリックメニュー、キーボードショートカット、ダブルクリック操作
- **高度なフィルタリング**: 保存検索、クイックフィルター、ファイル拡張子フィルター、最小変更数フィルター

### 3. プロジェクト構成
```
PullRequestManager.sln
├── AzureDevopsTool.Core/           # ビジネスロジック（DLL）
│   ├── PullRequestManager.Core.csproj
│   ├── Models/
│   │   ├── PullRequest.cs          # 拡張されたPRデータモデル
│   │   ├── Reviewer.cs
│   │   ├── WorkItem.cs
│   │   └── SavedSearch.cs          # 保存検索機能
│   └── Services/
│       ├── AzureDevOpsService.cs   # Azure DevOps API統合
│       ├── SettingsService.cs      # 設定管理
│       └── WindowsEncryptionService.cs # PAT暗号化
├── AzureDevopsTool.WPF/            # WPF UI
│   ├── PullRequestManager.WPF.csproj
│   └── MainWindow.xaml             # 分割ペイン、詳細表示、高度フィルター
└── AzureDevopsTool.CLI/            # CLI インターフェース
    └── PullRequestManager.CLI.csproj
```

### 4. 技術スタック
- **.NET 9.0** フレームワーク
- **WPF** (Windows Presentation Foundation) UI
- **Azure DevOps API** (Microsoft.TeamFoundationServer.Client)
- **Windows DPAPI** による暗号化
- **JSON** 設定ファイル
- **MVVM-style** データバインディング

### 5. 最新の変更
#### プロジェクト名変更
- アプリケーション名: "Azure DevOps Pull Requests" → "Pull Request Manager"
- ソリューションファイル: AzureDevopsTool.sln → PullRequestManager.sln
- プロジェクトファイル名:
  - AzureDevopsTool.Core.csproj → PullRequestManager.Core.csproj
  - AzureDevopsTool.WPF.csproj → PullRequestManager.WPF.csproj
  - AzureDevopsTool.CLI.csproj → PullRequestManager.CLI.csproj
- プロジェクト参照の更新済み

### 6. ビルド状況
- ✅ ビルド成功（警告1件: プラットフォーム固有暗号化サービス）
- ✅ 全機能動作確認済み
- ✅ プロジェクト名変更完了

### 7. 主要ファイル
- **MainWindow.xaml**: 分割ペインUI、詳細表示、フィルター機能
- **AzureDevOpsService.cs**: API統合、レビュワー取得、ワークアイテム取得
- **PullRequest.cs**: 拡張データモデル（アイコン、承認状況など）
- **SettingsService.cs**: 暗号化設定管理

## 開発コマンド
```bash
# ビルド
dotnet build PullRequestManager.sln

# 実行
dotnet run --project AzureDevopsTool.WPF/PullRequestManager.WPF.csproj
```

## 注意事項
- 一部プロジェクトフォルダ名は AzureDevopsTool のまま（動作に影響なし）
- 名前空間は AzureDevopsTool のまま（動作に影響なし）
- ユーザー表示名のみ "Pull Request Manager" に変更済み