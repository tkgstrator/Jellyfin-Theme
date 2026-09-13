# Jellyfin 12.0 Web UI の実測

テーマを書くために必要な、12.0 の Web クライアントの構造。**ここは実測の記録**なので、
Jellyfin を更新したら再検証する。方針は [design.md](../design.md)。

検証対象は `jellyfin/jellyfin:12.0` イメージの `/jellyfin/jellyfin-web/`（61 MB, webpack）と、
jellyfin-web の `v12.0` タグ。

## カスタム CSS / JS の入り口

| 層 | 場所 | 12.0 |
| --- | --- | --- |
| CSS（サーバー全体） | Dashboard → General → Custom CSS code | あり |
| CSS（クライアント単位） | Settings → Display → Custom CSS code | あり。サーバー側の後に適用され、後勝ち |
| HTML / JS | — | **無い** |

ユーザー側には「サーバー提供の CSS を無効化」するトグルがある。

```
DisableCustomCss      "Disable server-provided custom CSS code"
LabelCustomCss        "Custom CSS code"
LabelLocalCustomCss   "Custom CSS code for styling which applies to this client only."
```

`CustomHtml` / `CustomJavaScript` に相当するキーはバンドル内に 1 件も無い。JS を入れるには
プラグインが要る。

```console
$ docker exec <container> sh -c 'cd /jellyfin/jellyfin-web && grep -ohE "[Cc]ustom(Css|Html|JavaScript|Js)" *.js | sort | uniq -c'
    391 CustomCss
     28 customCss
```

`index.html` の中身は `<div id="reactRoot">` のみ。完全な SPA で、body にも html にも
ページ種別の手掛かりは無い。

## テーマトークン

配布 CSS を grep しても `--jf-*` は出てこない。MUI が**実行時に `<style>` を注入する**ため。
設定の実体は `main.jellyfin.bundle.js` にある。

```js
cssVariables: {
  cssVarPrefix: "jf",
  colorSchemeSelector: '[data-theme="%s"]',
  disableCssColorScheme: true
},
defaultColorScheme: "dark"
```

`@mui/material` は **6.5.0**（`v12.0` タグの package.json）。よって MUI の CSS 変数機構が
有効で、**色はセレクタ直叩きではなく変数で変えられる**。

生成される変数（MUI 6.5.0 の `shouldSkipGeneratingVar` から確定）。除外されるのは
`typography` / `mixins` / `breakpoints` / `direction` / `transitions` / `*sxConfig` /
`palette.mode|contrastThreshold|tonalOffset` のみで、残りは全て変数化される。

| グループ | 変数 | 既定値 |
| --- | --- | --- |
| 背景 | `--jf-palette-background-default` | `#101010` |
| | `--jf-palette-background-paper` | `#202020` |
| アクセント | `--jf-palette-primary-main` | `#00a4dc` |
| | `--jf-palette-secondary-main` | `#00a4dc` |
| 文字 | `--jf-palette-text-primary` / `-secondary` | |
| 操作 | `--jf-palette-action-hover` / `-focus` / `-selectedOpacity` | |
| 罫 | `--jf-palette-divider` | |
| 独自 | `--jf-palette-starIcon-main` | `#f2b01e` |
| エラー | `--jf-palette-error-main` | `#c62828` |
| 部品 | `--jf-palette-<Component>-*` | `AppBar` `Button` `Chip` `FilledInput` `Skeleton` `Slider` `TableCell` `Tooltip` `Alert` `Avatar` `LinearProgress` `SnackbarContent` `SpeedDialAction` `StepConnector` `StepContent` `Switch` の 16 種 |
| 角丸 | `--jf-shape-borderRadius` | |
| 影 | `--jf-shadows-0` 〜 `--jf-shadows-24` | |
| 余白 | `--jf-spacing` | |
| 重なり | `--jf-zIndex-*` | |
| 書体 | `--jf-font-h1` `--jf-font-body1` `--jf-font-button` … | `font` ショートハンド文字列 |

罠が 2 つ。

**フォントファミリ単体の変数は存在しない。** `prepareTypographyVars` はオブジェクト値の
variant だけを `font` ショートハンドに畳み込む。`typography.fontFamily`（`"Noto Sans", sans-serif`）
は文字列なので変数化されず、`--jf-font-fontFamily` は生成されない。書体を変えるには
`--jf-font-*` を個別に再定義するか、セレクタで直接当てる。

**ダーク/ライトの切替は `[data-theme="dark"]` / `[data-theme="light"]` 属性セレクタ。**
`prefers-color-scheme` でもクラスでもない。`disableCssColorScheme: true` なので
`color-scheme` 宣言も出力されない。

同梱テーマは `dark`（既定）/ `light` / `purplehaze`（`#230c33`, primary `#48c3c8`）/
`blueradiance`（`#0f3562`）/ `appletv`。

> **未確認** — 実行時に注入される `<style>` の実ダンプは取れていない。上記はバンドル内の
> 設定値と MUI 6.5.0 のソース規則からの導出。ライブラリを入れた 12.0 機で
> `getComputedStyle(document.documentElement)` を読んで裏を取ること。

## ルーティング

`src/RootAppRouter.tsx` は **`createHashRouter`**。URL は `https://host/web/#/movies` 形式で、
JS からは `location.pathname` ではなく **`location.hash`** を見る。

| 分類 | ルート |
| --- | --- |
| 動画 | `movies` `tv` `homevideos` `musicvideos` `boxsets` `livetv` |
| 音楽 | `music` `playlists` |
| 共通 | `home` `search` `list` `queue` `lyrics` `details` `mixed` `userprofile` `mypreferences*` |

**12.0 にはレイアウトが 2 系統ある。** `appSettings` の `layout` キー（`layoutManager.modern`）で
router 自体が切り替わる。

| | modern | legacy |
| --- | --- | --- |
| ライブラリページ | React（`LibraryPage.tsx`） | 旧 controller |
| ヘッダ | MUI `AppToolbar` + `AppDrawer` | `AppHeader`（`.skinHeader`） |

legacy では `movies` `tv` `music` `livetv` `home` が `legacyRoutes/user.ts` 側に乗る。

## ページ種別の DOM 露出

**ライブラリページは種別ごとに安定した `id` を持つ**（`LibraryPage.tsx` の `PAGE_IDS`。
配布バンドルでも実在を確認）。

```
#moviesPage  #tvshowsPage  #musicPage  #playlistsPage  #boxsetsPage
#booksPage   #liveTvPage   #homevideos #musicvideos    #mixed
```

`components/Page.tsx` の出力:

```html
<div id="musicPage" data-role="page"
     class="page backdropPage mainAnimatedPage libraryPage pageWithAbsoluteTabs withTabs"
     data-backdroptype="MusicArtist" data-title data-backbutton data-menubutton>
```

→ 動画/音楽の出し分けは `#moviesPage` 対 `#musicPage`、あるいは
`[data-backdroptype="Movie"]` 対 `[data-backdroptype="MusicArtist"]` で、**純 CSS で可能**。

**詳細ページだけは種別を出さない。** `#itemDetailPage` は映画でもアルバムでもアーティストでも
同一:

```html
<div id="itemDetailPage" data-role="page"
     class="page libraryPage itemDetailPage noSecondaryNavPage selfBackdropPage">
```

`itemDetails/index.js`（2201 行）が `page.classList` を触るのは `noBackdropTransparency` の
1 箇所のみ。`item.Type` は JS の分岐に使われるだけで DOM に書き戻されない。

補助材料として、詳細ページ内には種別依存で `.hide` が外れるセクションがある
（`#lyricsSection` `#musicVideosCollapsible` `#childrenCollapsible` `#castCollapsible`
`#scenesCollapsible` `#seriesScheduleSection`）。`:has(#lyricsSection:not(.hide))` で音楽を
拾える可能性はある。

> **未確認** — この `:has()` による判別は実行時挙動を検証していない。

`layout-desktop` / `layout-mobile` / `layout-tv` は **`<html>` 要素**に付く
（`document.documentElement.classList.add`）。body ではない。

## 主要な構造クラス

| 用途 | クラス |
| --- | --- |
| カード | `.card` `.cardBox` `.cardScalable` `.cardImageContainer` `.cardOverlayContainer` `.cardText` `.cardFooter` `.innerCardFooter` |
| 縦横比 | `.portraitCard` `.squareCard` `.backdropCard` `.overflowPortraitCard` `.overflowSquareCard` `.bannerCard` と対応する `.cardPadder-*` |
| セクション行 | `.verticalSection` `.sectionTitle` `.sectionTitleContainer` `.itemsContainer` `.horizontalItemsContainer` `.scrollX` `.emby-scrollbuttons` |
| 詳細ヘッダ | `.detailPageWrapperContainer` `.detailPagePrimaryContainer` `.detailRibbon` `.detailImageContainer` `.detailLogo` `.itemBackdrop` `.nameContainer` `.itemMiscInfo` `.mainDetailButtons` |
| ナビ（legacy） | `.skinHeader` `.headerTop` `.headerLeft` `.headerRight` `.mainDrawer` `.navMenuOption` |
| リスト | `.listItem` `.listItemBody` `.listItemBodyText` `.listItemImage` `.listItemIndicators` / 表は `.detailTable` `.detailTableBodyCell` |

## クラスの寿命

静的 CSS 全体で **3540 クラス、うち 393（約 11%）が 20 桁 hex のハッシュ名**
（CSS Modules、例 `.a000b79c5c06db187695`）。**ビルドごとに変わるので狙わない。**

残り約 89% は旧 Emby 由来の意味のある名前で、10.x から継続しており安定している。

`.Mui*` は静的 CSS に **0 件**。MUI は emotion のランタイム注入（`css-xxxx`、これもビルド依存）。
ただし `MuiAppBar-root` 等のグローバルクラスは DOM 上には出るため、セマンティッククラス経由の
指定は可能。

> **未確認** — ランタイム DOM 上の `.Mui*` は実機で確認していない。

## 再検証の手順

Jellyfin を更新したら、少なくとも次を確認する。

```console
# カスタム CSS 欄が残っているか
$ docker exec theme-jellyfin sh -c 'cd /jellyfin/jellyfin-web && grep -c CustomCss *.js | grep -v ":0"'

# CSS 変数のプレフィックスと配色セレクタが変わっていないか
$ docker exec theme-jellyfin sh -c 'cd /jellyfin/jellyfin-web && grep -o "cssVarPrefix:\"[a-z]*\"" main.jellyfin.bundle.js'

# ライブラリページの id が残っているか
$ docker exec theme-jellyfin sh -c 'cd /jellyfin/jellyfin-web && grep -o "moviesPage\|musicPage\|itemDetailPage" *.js | sort -u | head'
```

ブラウザ側では、DevTools で `document.documentElement.dataset` と
`getComputedStyle(document.documentElement).getPropertyValue('--jf-palette-background-default')`
を見るのが早い。
