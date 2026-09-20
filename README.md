# RoleShuffle

R.E.P.O.向け役職MODのソース管理用リポジトリです。
プレイヤー向けの仕様・設定は[配布用README](package/README.md)、
バージョンごとの変更点は[CHANGELOG](package/CHANGELOG.md)を参照してください。

## v4.5.0開発

`feature/roleshuffle-4.5.0-overhaul`ブランチを専用worktreeで開発しています。
現行の`StageRoles`フォルダ、`main`、既存のDLL・ZIP・ゲーム導入先は保持します。
仕様・分岐元・検証状況は[開発記録](docs/OVERHAUL_4.5.0.md)を参照してください。
Tank・Runner・Lifter・Courier・Kingの更新はv4.5.0の標準仕様です。旧仕様への切り替え設定は廃止し、設定移行時にバックアップを作成します。
StinkerとBomberは移動による自動発生を維持し、任意停止操作は追加しません。

```powershell
dotnet run --project tools/OverhaulChecks/OverhaulChecks.csproj -c Release
pwsh -NoProfile -File tools/Test-BaseUpgradeEligibility.ps1
```

## お問い合わせ

ご質問、不具合報告、ご意見は[GitHub Issues](https://github.com/CapacityDown/RoleShuffle/issues)へお寄せください。
GitHubアカウントでログインするとIssueを投稿できます。

## ビルド

.NET SDK 9.0.xを使用します。`global.json`でSDKの範囲を指定しています。
NuGet依存関係は`StageRoles.csproj`に定義し、MenuLibのビルド参照は
`lib/MenuLib.dll`を同梱しています。

ビルド前に`RoleMenu.cs`の`RoleUiBuildNumber`を1増やしてください。
失敗したビルドもカウントします。

```powershell
dotnet restore StageRoles.csproj
dotnet build StageRoles.csproj -c Release --no-restore
```

出力先は`bin/Release/netstandard2.1/RoleShuffle.dll`です。
ビルド後は`pwsh -NoProfile -File tools/Test-GameFieldAccess.ps1`を実行し、実ゲームDLLの非公開フィールドを直接参照していないことを確認してください。ビルド用GameLibsの公開範囲と実ゲームの公開範囲は異なります。ゲームが別の場所にある場合は`-GameAssembly`で`Assembly-CSharp.dll`のパスを指定します。
このプロジェクトのビルドではゲームへの自動配備を行いません。
ゲームで使用する際は、`package/manifest.json`に記載された依存MODを別途導入してください。

## プレイヤー向けツールの検証

```powershell
dotnet run --project tools/UtilityChecks/UtilityChecks.csproj -c Release
dotnet run --project tools/UtilityRuntimeChecks/UtilityRuntimeChecks.csproj -c Release
```

検証範囲と実機で確認する操作は[UtilityChecks](tools/UtilityChecks/README.md)を参照してください。
スクロール処理のゲーム本体への適合・ページごとの移動量は[ScrollChecks](tools/ScrollChecks/README.md)で検証します。
現在の設定は1目盛りで本文3行分です。[build 420の実操作動画・比較表](docs/verification/scroll-build420/README.md)は、旧設定（1行分）の検証記録です。

多言語の設定保存・翻訳・ホスト値保持は[LocalizationChecks](tools/LocalizationChecks/README.md)で検証します。
表情による能力発動とESCメニュー遷移は[ExpressionChecks](tools/ExpressionChecks/README.md)で検証します。
Roles UIのロール／Base Upgrade切替・ロールのプリセット・REPOConfigとの設定共有・保存・ホスト権限は[RoleSettingsChecks](tools/RoleSettingsChecks/README.md)で検証します。
翻訳の編集方法は[Localization](Assets/Localization/README.md)、同梱フォントの生成方法とライセンスは[Fonts](Assets/Fonts/Localization/README.md)を参照してください。

## 管理するファイル

- C#ソース、プロジェクト設定、開発用スクリプト
- 採用済みの役職画像原本、実行用画像、フォント素材
- 配布用README、CHANGELOG、manifest、パッケージアイコン
- 開発・リリース手順

DLLのビルド出力、ZIP、生成PDF、一時ファイル、画像の旧案や比較画像は
`.gitignore`で除外します。`lib/MenuLib.dll`はビルド参照として例外的に管理します。
第三者ライブラリや素材の権利は各権利者に帰属します。

役職画像の実行用PNGは既に登録されているため、通常のビルドで画像生成は不要です。
画像を更新する場合は[透過画像の説明](Assets/role-emblems-semibot-v1/transparent/README.md)を参照し、
Python・Pillow・NumPyで`tools/build_transparent_role_emblems.py`を実行します。

## Gitでの変更管理

リモートは`https://github.com/CapacityDown/RoleShuffle.git`、基準ブランチは`main`です。
変更前に`git status`と`git pull --ff-only`で状態を確認し、作業内容ごとのブランチを使用します。
差分を確認して関連するファイルだけをコミットし、GitHubへpushしてください。
`main`への取り込み前にビルド結果と変更内容を確認します。

バージョンはプロジェクト・プラグイン・manifest・CHANGELOGの先頭見出しを揃えます。
CHANGELOGには前リリースからの最終的な変更点をまとめ、同バージョン内の修正経緯は記載しません。
詳しくは[リリース文書のルール](RELEASE_DOCUMENT_GUIDE.md)を参照してください。

既存のリリース履歴はCHANGELOGに保持しています。
Gitの履歴はこのリポジトリへの初回登録から記録します。
