# Minge2026Spring_Team4

春みんげ2026　チーム４👴

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
