# Steam 実績 / Cloud 設定

## 実績

Steamworks 管理画面の Stats & Achievements に、次の API Name を大文字・小文字も一致させて登録する。  
表示名、説明、解除前後の画像を設定して Publish する。

| API Name | 条件 |
| --- | --- |
| ACH_ENDING_A | reachedEndingIds に Ending_A |
| ACH_ENDING_B | reachedEndingIds に Ending_B |
| ACH_ENDING_C | reachedEndingIds に Ending_C |
| ACH_ENDING_D | reachedEndingIds に Ending_D |
| ACH_ENDING_E | reachedEndingIds に Ending_E |
| ACH_ENDING_F | reachedEndingIds に Ending_F |
| ACH_ENDING_G | reachedEndingIds に Ending_G |
| ACH_ENDING_H | reachedEndingIds に Ending_H |
| ACH_ENDING_I | reachedEndingIds に Ending_I |
| ACH_ENDING_J | reachedEndingIds に Ending_J |
| ACH_ENDING_K | reachedEndingIds に Ending_K（追加ステージ） |
| ACH_HIDDEN_ITEM_1 | GetItem1 が true |
| ACH_HIDDEN_ITEM_2 | GetItem2 が true |

## Steam Auto-Cloud

Unity と外部 exe が直接更新するファイルを、Steam クライアントが起動前・終了後に同期する方式を使用する。  
ISteamRemoteStorage による別コピーは作らない。  
以下の管理画面設定が完了するまでは Cloud 同期は有効にならない。

1. Steamworks > Application > Steam Cloud で容量を設定。初期値の提案は Byte quota per user = `10485760`（10 MiB）、Number of files allowed per user = `10`。実セーブの最大サイズに合わせて調整する。
2. 下表の Root Paths を登録。OS は Windows、Recursive はすべて OFF。
3. Save と Publish を実行。公開済みアプリで事前検証する場合は開発者限定を有効にする。

| Root | Subdirectory | Pattern |
| --- | --- | --- |
| WinAppDataLocalLow | PandD_org/ClubPandD | GameSave.json |
| WinAppDataLocalLow | PandD_org/ClubPandD | InternalParameters.json |
| WinAppDataLocalLow | PandD_org/ClubPandD | FMODSettings.json |
| App Install Directory | <実際のexe名>_Data/StreamingAssets/I_gonna_be_the_tresure_hunter | CharactersMoraleValue.json |

**保存先確認:** Unity の `Tools > Steam > Show Cloud Save Paths` で実際の persistentDataPath を確認する。上表の `PandD_org/ClubPandD` は現在の外部ゲーム側に固定されたパス。Company Name は `PandD.org` なので、Unity 側の実パスが一致することを確認する。異なる場合は外部ゲームと Unity の保存先を揃えてから Cloud を設定する。古い別フォルダを同時に同期対象に追加しない。