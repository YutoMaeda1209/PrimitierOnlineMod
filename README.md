# PrimitierOnlineMod

## DevContainer について

このリポジトリには、VS Code DevContainer の設定が含まれています。DevContainer を利用することで、開発環境を簡単に構築・共有できます。

### 概要

- Ubuntu ベースのコンテナ上で.NET 6.0 SDK がインストールされます。
- 必要な VS Code 拡張機能（C# Dev Kit, Prettier, EditorConfig など）が自動でインストールされます。

### 使い方

1. [Docker](https://www.docker.com/)と[Visual Studio Code](https://code.visualstudio.com/)をインストールしてください。
2. `.devcontainer/.env.example` をコピーして `.devcontainer/.env` を作成し、`PRIMITIER_INSTALLATION_PATH` に Primitier のインストールパスを設定してください。
3. VSCode でこのリポジトリを開き、`Reopen in Container`（コンテナで再オープン）を選択します。
4. DevContainer が起動したら、`dotnet restore`が自動で実行されます。

### 参考

- [DevContainer 公式ドキュメント](https://containers.dev/)
