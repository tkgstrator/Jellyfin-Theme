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

注入経路は**自作プラグイン**（後述の「配布と注入」）。12.0 に Custom HTML /
Custom JavaScript の欄は無いので、プラグイン以外の道は無い。

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

## 6. 配布と注入

**自作プラグイン 1 本で配る。** [JavaScript Injector](https://github.com/n00bcodr/Jellyfin-JavaScript-Injector)
に乗る手もあるが、他人のプラグインに依存すると壊れたときに自分で直せない。

利用者の手順は「プラグインを入れる」だけ。CSS の貼り付けも Injector の設定も要らない。
CSS と JS はプラグインの埋め込みリソースとして同梱し、プラグインは `index.html` への
`<link>` / `<script>` 挿入だけを行う。

ABI は **`net10.0` / Jellyfin 12.0 のみ**。10.11 は対応対象外なので TFM は 1 本。

### index.html をディスク上で書き換えてはいけない

web root は Docker だと書けないことがあり、書けても web クライアントの更新で消える。

代わりに **`IStartupFilter` で ASP.NET Core のミドルウェアを挿し、リクエスト時に
レスポンス本文を書き換える**。Injector も同じ方式に移行済みで、10.11 と 12.0 の両方で
無改変に動く実績がある（`Services/ScriptInjectionStartupFilter.cs`）。

実装で外せない点。

| | 理由 |
| --- | --- |
| ミドルウェアは `next(app)` より前に登録して最外側で回す | 下の `Accept-Encoding` 除去が効くのは最外側のときだけ |
| リクエストから `Accept-Encoding` / `Range` / `If-Range` を落とす | 圧縮済み本文や 206 partial は書き換えられない。206 が素通りすると長さが壊れる |
| `GET` のみ。`HEAD` 等は素通し | 本文の無い応答をバッファすると `Content-Length` が 0 相当になる |
| 対象は `/web`・`/web/`・`/web/index.html` を `EndsWith` で判定 | base-url 配下（`/jellyfin/web/`）でも当たる |
| `200` かつ `text/html` 以外は素通し | 304・リダイレクト・静的ファイルを壊さない |
| 書き換え後に `ETag` / `Last-Modified` / `Accept-Ranges` を消し `ContentLength` を再設定 | 本文が変わった時点で元の検証子は無効 |
| 例外は握って元の HTML を返す | index.html を落とすと UI が全滅する |
| マーカーコメントで冪等にする | 二重注入と、旧方式で既に書き込まれた index.html を避ける |

## 7. 未決

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

これは開発中の確認用。**配布はプラグイン経由**（方針 6）で、利用者に CSS を貼らせる
運用はしない。クライアント単位の Custom CSS 欄（Settings → Display）はサーバー側の後に
適用されるので、共用機で試すときはそちらを使うと他の利用者に影響しない。
