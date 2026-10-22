# CLAUDE.md

このリポジトリで作業するエージェント向けの指針。

## プロジェクトの目的

Jellyfin 12.0 の Web クライアントを、**動画は Netflix ライク、音楽は Apple Music ライク**
に見せるテーマ。配布はプラグイン 1 本で、利用者の手順は「プラグインを入れる」だけ。

**設計の権威は [docs/design.md](docs/design.md)。** 根拠になる Web UI の実測は
[docs/research/jellyfin-12-web.md](docs/research/jellyfin-12-web.md)。判断に迷ったら
先にこの 2 つを読む。

CI・リリース経路は姉妹プラグイン [Jellyfin-AmazonMusic-Metadata][amazon] と共有している。
ワークフローやスクリプトを触るときは向こうと揃っているか確認する。

## 決定済みの設計（勝手に変えない）

1. **対象は Jellyfin 12.0 のみ。TFM は `net10.0` 1 本。** 10.11 は Emby 由来 UI と
   React/MUI が混在する移行途中の世代で、セレクタが二重になる。compose の
   `legacy` プロファイル（:8099）は比較用に置いてあるだけ。
2. **JS がやってよいのは `#itemDetailPage` に `data-item-type` を書き戻すことだけ。**
   見た目は 100% CSS に寄せる。JS が描画に踏み込むと Jellyfin が DOM を変えるたびに壊れる。
3. **CSS Modules のハッシュ名（`.a000b79c5c06db187695`）と emotion のランタイムクラス
   （`.css-1x2y3z`）は使わない。** ビルドごとに変わる。狙ってよいのは旧 Emby 由来の
   意味のある名前（`.card` `.detailRibbon` `.listItem`）、ページ id、MUI の
   セマンティッククラス。
4. **`index.html` をディスク上で書き換えない。** web root は Docker だと書けないことがあり、
   書けても web クライアントの更新で消える。`IStartupFilter` でミドルウェアを挿し、
   リクエスト時にレスポンス本文を書き換える。実装上の制約は design.md §6 の表が全部。
5. **配色は MUI の CSS 変数（`--jf-*`）の再定義で通す。** ダーク/ライトは
   `[data-theme="dark"]` / `[data-theme="light"]` 属性であって `prefers-color-scheme`
   ではない。両方に定義を置く。
6. **リリース成果物は `scripts/package.sh` が作る。** jprm / `build.yaml` は使わない。
   メタ情報は `scripts/meta.template.json` が単一の出所。
7. **UI 文言（設定画面）は英語。** README / docs / コミットメッセージ本文は日本語でよい。

## リポジトリ構造

```
.devcontainer/            Dev Container（app + Jellyfin 12.0 + Jellyfin 10.11）
.github/workflows/        integration.yaml, deployment.yaml
docs/                     design.md（設計方針）, research/（Web UI 実測）, tokens.md
scripts/                  package.sh, manifest.py, deploy.sh, meta.template.json
Jellyfin.Plugin.Theme/
  Plugin.cs               BasePlugin<PluginConfiguration>, IHasWebPages
  PluginServiceRegistrator.cs  IStartupFilter の DI 登録
  Configuration/          PluginConfiguration.cs, configPage.html（埋め込みリソース）
  Services/
    ThemeInjection.cs           注入の純粋な文字列処理（ASP.NET 非依存・テスト対象）
    ThemeInjectionStartupFilter.cs  ミドルウェア本体
  Api/ThemeAssetController.cs   埋め込んだ CSS/JS の配信口
  Resources/              theme.css, theme.js（埋め込みリソース）
tests/Jellyfin.Plugin.Theme.Tests/
Directory.Build.props     バージョンと Jellyfin バージョン/ABI
```

**`Services/ThemeInjection.cs` は ASP.NET の型を参照しない。** サーバー上でステップ実行
できないので、判定と文字列組み立てをここに寄せてユニットテストで検証する。
`ThemeInjectionStartupFilter` に残すのはパイプライン操作だけ。

## ビルド・テスト

```bash
dotnet build                          # net10.0
dotnet test                           # xunit v3 / Microsoft.Testing.Platform
dotnet format --verify-no-changes     # CI と同じ書式チェック
./scripts/deploy.sh                   # 開発用 Jellyfin (:8098) に反映して再起動
./scripts/package.sh [version]        # dist/ にリリース成果物
```

### 環境まわりの既知の事情

- **`dotnet test` は .NET 10 SDK で VSTest が使えない。** `global.json` で
  Microsoft.Testing.Platform に opt-in している。テストプロジェクトは
  `<OutputType>Exe</OutputType>` かつ xunit v3。
- **プラグイン本体は Jellyfin 参照を `ExcludeAssets=runtime` で参照する。**
  テストプロジェクトは通常参照する。
- **dev サーバーを勝手に立てない。** 確認は `./scripts/deploy.sh` と :8098 で行う。

## コーディング規約

- `TreatWarningsAsErrors=true`。警告を残さない。
- **public メンバーには XML ドキュメントコメントを書く。**
- **コード内のコメントと識別子は英語。** 日本語はドキュメントと会話のみ。
- ログは Serilog 形式の構造化ログ。文字列連結や補間で組み立てない。

## ブランチとリリース

```
feature/*  ──PR──▶  develop  ──マージ──▶  master  ──v* タグ──▶  正式リリース
```

- ワークフローは `integration.yaml` と `deployment.yaml` の 2 つだけ。
- `deployment.yaml` は `integration.yaml` を `workflow_call` で呼んでから公開する。
- **manifest はチャンネルごとに分ける。** まとめると dev ビルド `0.1.0.42` が安定版
  `0.1.0.0` より上に並び、安定版利用者が自動更新で dev を掴む。

## コミット

Conventional Commits（`.commitlintrc.yaml`、header は 128 文字まで）。
type は `build/ui/ci/docs/feat/fix/perf/refactor/revert/format/test/chore`。

## やらないこと

- `index.html` をディスク上で書き換えない。
- ハッシュ由来のクラス名を CSS で狙わない。
- JS で描画に踏み込まない（属性の書き戻しのみ）。
- Jellyfin のランタイムアセンブリを配布物に含めない。
- `dist/`, `bin/`, `obj/`, `media/` の中身をコミットしない。

[amazon]: https://github.com/tkgstrator/Jellyfin-AmazonMusic-Metadata
