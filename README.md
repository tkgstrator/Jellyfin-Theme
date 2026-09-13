# Jellyfin Theme

動画は Netflix ライク、音楽は Apple Music ライクに見せる **Jellyfin 12.0 用テーマ**。

CSS の貼り付けも他のプラグインの設定も要りません。プラグインを入れるだけです。

> **Jellyfin 12.0 専用。** 10.11 には対応していません。

## インストール

1. Jellyfin の管理画面で **ダッシュボード → プラグイン → リポジトリ** を開く
2. 次の URL を追加する

   ```
   https://tkgstrator.github.io/Jellyfin-Theme/manifest.json
   ```

3. **カタログ**タブから *Netflix / Apple Music Theme* をインストールする
4. Jellyfin を再起動し、ブラウザを再読み込みする

開発版を試す場合は `manifest.json` の代わりに
`https://tkgstrator.github.io/Jellyfin-Theme/dev/manifest.json` を登録してください。
`develop` ブランチの自動ビルドが並びます。

## 仕組み

テーマのスタイルシートとスクリプトは、**リクエスト時に `index.html` へ差し込まれます**。
web クライアントのファイルには一切書き込まないので、Jellyfin や web クライアントを
更新してもテーマが消えません。

スクリプトが行うのは 1 点だけです。詳細ページは映画でもアルバムでも同じ DOM なので、
種別を `data-item-type` 属性として書き戻します。見た目はすべて CSS 側にあります。

## うまく動かないとき

web クライアントが真っ白になるなど問題が出た場合は、
**ダッシュボード → プラグイン → Netflix / Apple Music Theme** で
*Apply the theme to the web client* のチェックを外して保存し、ページを再読み込みしてください。
プラグインをアンインストールしなくても既定の外観に戻ります。

## 開発

設計方針は [docs/design.md](docs/design.md)、Jellyfin 12.0 の Web UI の実測は
[docs/research/jellyfin-12-web.md](docs/research/jellyfin-12-web.md) にあります。

```bash
dotnet build
dotnet test
./scripts/deploy.sh      # 開発用 Jellyfin (:8098) に入れて再起動
./scripts/package.sh     # dist/ にリリース成果物
```

## ライセンス

[GPL-3.0](LICENSE)。Jellyfin 本体と同じライセンスです。
