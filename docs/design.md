# 設計方針

動画は Netflix ライク、音楽は Apple Music ライクに見せる Jellyfin 12.0 用テーマ。
根拠になる実測は [research/jellyfin-12-web.md](research/jellyfin-12-web.md)。

## 対象

**Jellyfin 12.0 のみ。** 10.11 は Emby 由来 UI と React/MUI が混在する移行途中の世代で、
セレクタが二重になる。10.11 機（:8099）は比較用に置いてあるだけで、対応対象ではない。

## 1. JS の役割は 1 点に限定する

CSS だけでは詳細ページの種別が判別できない。`#itemDetailPage` は映画でもアルバムでも
同じ DOM で、`item.Type` はどこにも書き戻されない。ここだけ JS が要る。

**JS がやってよいのは「`#itemDetailPage` に `data-item-type` を書き戻す」ことだけ。**
見た目は 100% CSS に寄せる。

理由: JS が描画に踏み込むと、Jellyfin が DOM を変えるたびに壊れる。属性を付けるだけなら
壊れる面が小さく、壊れても「配色が既定に戻る」で済む。

```
location.hash から itemId を取る
  → 種別を引く
  → #itemDetailPage に data-item-type を付ける
  → hashchange で貼り直す
```

注入経路は [JavaScript Injector](https://github.com/n00bcodr/Jellyfin-JavaScript-Injector)
（12 系向けの manifest を配っている）。12.0 に Custom HTML / Custom JavaScript の欄は無いので、
プラグイン以外の道は無い。

## 2. 出し分けはライブラリページの id で行う

ライブラリページには種別ごとに安定した id がある。ここは JS 不要。

```css
:root                                        { /* 共通トークン */ }

#moviesPage, #tvshowsPage, #boxsetsPage      { /* Netflix */ }
#musicPage,  #playlistsPage                  { /* Apple Music */ }

#itemDetailPage[data-item-type="Movie"]      { /* Netflix */ }
#itemDetailPage[data-item-type="MusicAlbum"] { /* Apple Music */ }
```

`[data-backdroptype="Movie"]` / `[data-backdroptype="MusicArtist"]` も使える。

## 3. 色は変数、構造はセレクタ

MUI 6.5.0 の CSS 変数機構が有効（プレフィックス `jf`）なので、**配色は変数の再定義だけで通る**。

```css
#musicPage {
  --jf-palette-background-default: #000;
  --jf-palette-primary-main: #fa243c;
  --jf-shape-borderRadius: 8px;
}
```

ダーク/ライトは `[data-theme="dark"]` / `[data-theme="light"]` 属性で切り替わる。
`prefers-color-scheme` ではないので、両方に定義を置く。

書体だけは変数が無い（`--jf-font-fontFamily` は生成されない）。`--jf-font-h1` 等を
個別に上書きするか、セレクタで直接当てる。

## 4. 狙ってよいセレクタ / 狙ってはいけないセレクタ

| | 例 | 可否 |
| --- | --- | --- |
| 旧 Emby 由来の意味のある名前 | `.card` `.detailRibbon` `.listItem` | **狙う**。10.x から継続、静的 CSS の約 89% |
| ページ id | `#musicPage` `#itemDetailPage` | **狙う**。安定 |
| MUI のセマンティッククラス | `.MuiAppBar-root` | 狙ってよい |
| CSS Modules のハッシュ名 | `.a000b79c5c06db187695` | **禁止**。ビルドごとに変わる（約 11%） |
| emotion のランタイムクラス | `.css-1x2y3z` | **禁止**。同上 |

## 5. CSS で届かないもの

先に把握しておく。ここを勘違いすると設計をやり直すことになる。

| やりたいこと | 可否 |
| --- | --- |
| 色・角丸・影・余白・タイポ | 変数で一括 |
| hover 拡大＋メタ情報オーバーレイ（Netflix 風） | `.cardScalable` / `.cardOverlayContainer` で可 |
| 正方形アートワーク（Apple Music 風） | `.squareCard` が既にある。相性が良い |
| **16:9 サムネイル（Netflix 風）** | **不可**。既定は縦ポスター `.portraitCard` で、`aspect-ratio` で切ると絵が破綻する。使う画像種別はライブラリ設定側の話で、CSS からは変えられない |
| **ヒーローバナーの新設** | **不可**。無い要素は作れない。やるなら JS が要り、方針 1 と衝突する |

16:9 にしたい場合は、ライブラリ側で Thumb / Backdrop を持たせたうえで CSS を当てる。
テーマ単体では完結しない。

## 6. 未決

- **ヒーローバナーを諦めるか、方針 1 を緩めるか。** Netflix らしさの核なので、
  諦めると「黒くて赤い Jellyfin」止まりになる可能性がある。まず諦めた状態で作り、
  物足りなければ再考する。
- **アートワークからの色抽出（Apple Music の背景ぼかし）をやるか。** CSS だけでは不可能。
  やるなら JS で、方針 1 を緩めることになる。

## 開発の回し方

テスト用の Jellyfin 12.0 は :8098（10.11 は `--profile legacy` で :8099）。

ビルドした CSS を :4173 で配って、Jellyfin の Custom CSS 欄から読み込むと反映が速い。

```css
@import url("http://localhost:4173/theme.css");
```

本番へは中身を貼り付けるか、GitHub Pages 等に置いた URL を `@import` する。
クライアント単位の Custom CSS 欄（Settings → Display）はサーバー側の後に適用されるので、
試すときはそちらを使うと他の利用者に影響しない。
