# デザイントークンの実測

Netflix と Apple Music から**数値だけ**を取り出して、Jellyfin の既定値との差分を見るための表。

**他社の CSS / JS は持ち込まない。** どちらも React SPA でクラス名はハッシュ化されており、
`.card` や `#musicPage` にマッピングできないので読んでも移植先が無い。加えてこのリポジトリは
public なので、再配布はライセンス上の問題になる。欲しいのは以下の十数個の値だけ。

空欄は未実測。**推測値を書かない。** 埋めるときは必ず測った値を入れ、測り方が特殊なら備考に残す。

## 測り方

Playwright MCP（`playwright@qtmleap-plugins`、`devflow:playwright` エージェント経由）で
実ページを開き、`getComputedStyle` を読むのが最も確実。スクリーンショットの目視は
色が sRGB 変換や JPEG 圧縮でずれるので、補助にとどめる。

```js
// 対象要素を選んでから
const el = document.querySelector(SELECTOR);
const s = getComputedStyle(el);
({
  bg:       s.backgroundColor,
  color:    s.color,
  radius:   s.borderRadius,
  font:     `${s.fontSize}/${s.lineHeight} ${s.fontWeight} ${s.fontFamily}`,
  gap:      s.gap,
  ratio:    `${el.clientWidth}x${el.clientHeight}`,
  duration: s.transitionDuration,
  easing:   s.transitionTimingFunction,
})
```

hover の拡大率は `transform` を hover 前後で読むか、`getBoundingClientRect()` の差で出す。

### 注意

- **Netflix のブラウズ画面はログインが要る。** 認証情報をこのリポジトリに置かない。
  Playwright で開くなら既存のログイン済みプロファイルを使うか、手元のブラウザで測る
- **Apple Music は music.apple.com がログイン無しでもある程度見える。** アルバムページと
  ブラウズは測れる
- Apple 側は Human Interface Guidelines という一次資料があるので、実測と突き合わせる。
  この環境には `apple-hig-review` スキルがある
- Netflix にはデザインシステムの公開が無いので実測のみ

## 配色

Jellyfin の既定は [research/jellyfin-12-web.md](research/jellyfin-12-web.md) の実測値。

| 軸 | Netflix | Apple Music (dark) | Apple Music (light) | Jellyfin 既定 |
| --- | --- | --- | --- | --- |
| 背景 | | | | `#101010` |
| 面（カード・パネル） | | | | `#202020` |
| アクセント | | | | `#00a4dc` |
| 本文 | | | | |
| 副次テキスト | | | | |
| 罫・境界 | | | | |
| hover の重ね | | | | |

Jellyfin 側の対応変数は `--jf-palette-background-default` / `-background-paper` /
`-primary-main` / `-text-primary` / `-text-secondary` / `-divider` / `-action-hover`。

## 形状・余白

| 軸 | Netflix | Apple Music | Jellyfin 既定 |
| --- | --- | --- | --- |
| 角丸（カード） | | | |
| 角丸（ボタン） | | | |
| 影 | | | |
| カードの縦横比 | | | `.portraitCard` = 2:3 |
| カード間の間隔 | | | |
| セクション間の余白 | | | |
| 行の左右パディング | | | |

`--jf-shape-borderRadius` / `--jf-shadows-*` / `--jf-spacing` が対応。

## タイポグラフィ

| 軸 | Netflix | Apple Music | Jellyfin 既定 |
| --- | --- | --- | --- |
| 書体スタック | | | `"Noto Sans", sans-serif` |
| セクション見出し | | | |
| カードのタイトル | | | |
| 本文 | | | |
| 字間 | | | |

Jellyfin には書体ファミリ単体の変数が無い（`--jf-font-fontFamily` は生成されない）ので、
`--jf-font-h1` 等を個別に上書きするか、セレクタで直接当てる。

## 挙動

| 軸 | Netflix | Apple Music | Jellyfin 既定 |
| --- | --- | --- | --- |
| hover の拡大率 | | | |
| hover の遅延 | | | |
| イージング | | | |
| オーバーレイの出方 | | | |

## 測る対象

どのページのどの要素を測ったかを残す。同じ「カード」でもトップと一覧で値が違うことがある。

| # | サービス | ページ | 要素 | 測定日 |
| --- | --- | --- | --- | --- |
| | | | | |
