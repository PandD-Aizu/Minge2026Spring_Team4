# Minge2026Spring_Team4

春みんげ2026　チーム４👴

## 開発環境のセットアップ

GitをインストールしてPATHを通し、リポジトリをclone / pullした後にUnity `6000.3.6f1`で開いてください。
Steamworks.NETを含むGit依存パッケージは、`Packages/manifest.json`と`Packages/packages-lock.json`に従って各自の環境へ自動取得されます。初回取得にはネット接続が必要です。
Steamworks.NETのunitypackageのインポートや、Assetsへの手動配置は不要です。詳細は[Steam設定](Documentation/SteamSetup.md)を参照してください。

## コーディング規約
### 命名規則
* クラス / 構造体名: パスカルケース(例: PlayerController)
* メソッド名: パスカルケース(例: Attack())
* プロパティ: パスカルケース(例: CurrentHealth)
* public変数: パスカルケース(例: MaxSpeed)
* private変数: _ + キャメルケース(例: _currentScore)
* ローカル変数: キャメルケース(例: damageAmount)
* 引数: キャメルケース(例: targetPosition)
* インターフェイス: I + パスカルケース(例: IDamageable)
* 定数: スネークケース(例: MAX_HEALTH)

### アーキテクチャ
* MVP + Clean Architecture
    * 基本的なフォルダ構成は、PixelPileと同じです。
    * いくつかの項目において、PixelPileより細かくフォルダ分けしました。
    * 各フォルダに何を書くべきかをREADME.mdに記しておきました。

### 技術スタック
* DI: VContainer
* Reactive: R3
* Async: UniTask
* Asset Management: Addressables

## GitHub Actionsによるビルド

タグをpushすると、Unity `6000.3.6f1`でWindows向けビルドが実行されます。
ビルド完了後、タグと同名のGitHub Releaseが作成され、`Minge2026Spring_Team4-Windows.zip`が添付されます。

初回のみ、リポジトリの Settings > Secrets and variables > Actions に次のRepository Secretsを登録してください。

* `UNITY_EMAIL`: Unity IDのメールアドレス
* `UNITY_PASSWORD`: Unity IDのパスワード
* `UNITY_LICENSE`: Unityライセンスファイルの内容

実行例:

```bash
git tag -a v1.0.0 -m "Realease v1.0.0"
git push origin v1.0.0
```
