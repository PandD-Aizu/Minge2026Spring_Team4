using UnityEngine;

namespace Workspace.Koto.Scripts.JSONLoadTest
{
    public class PlayerStatusManager : MonoBehaviour
    {
        private PlayerStatus status;
        
        private void Start()
        {
            // jsonに変換するオブジェクトを作成
            status = new PlayerStatus(100, 20, new Vector3(1, 2, 3));
            
            // jsonファイルにパース(変換)する
            // 第２引数は見やすくするためのインデントを付けるかどうかのオプション
            var json = JsonUtility.ToJson(status, true);
            
            // 結果を出力
            Debug.Log($"Output: {json}");
        }
    }
}