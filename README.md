# Azure DevOps Pull Request Tool

Azure DevOpsでPull Requestの一覧をGrid上に表示するツールです。

## プロジェクト構成

- **AzureDevopsTool.Core**: Azure DevOps APIとの通信ロジックを含むクラスライブラリ
- **AzureDevopsTool.WPF**: WPFを使用したGUIアプリケーション
- **AzureDevopsTool.CLI**: コマンドライン インターフェース

## 必要な設定

Azure DevOpsに接続するために以下の情報が必要です：

- **Organization**: Azure DevOpsの組織名
- **Project**: プロジェクト名
- **Repository**: リポジトリ名
- **Personal Access Token**: 認証用のPersonal Access Token

## WPFアプリケーションの使用方法

1. アプリケーションを起動
2. 上部の設定フィールドに必要な情報を入力
3. "Load Pull Requests"ボタンをクリック
4. Pull Requestの一覧がDataGridに表示されます

## CLIの使用方法

```bash
AzureDevopsTool.CLI.exe <organization> <project> <repository> <personal-access-token>
```

### 例
```bash
AzureDevopsTool.CLI.exe myorg myproject myrepo abcdef1234567890
```

## ビルド方法

```bash
dotnet build
```

## 実行方法

### WPFアプリケーション
```bash
dotnet run --project AzureDevopsTool.WPF
```

### CLIアプリケーション
```bash
dotnet run --project AzureDevopsTool.CLI -- <organization> <project> <repository> <pat>
```