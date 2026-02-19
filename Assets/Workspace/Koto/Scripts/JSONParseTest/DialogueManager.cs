using UnityEngine;

namespace Workspace.Koto.Scripts.JSONLoadTest
{
    public class DialogueManager : MonoBehaviour
    {
        [SerializeField, Tooltip("読み込むJSONファイルをここにアタッチ")] 
        private TextAsset json;
        
        private Dialogue dialogue;
        
        private void Start()
        {
            // JSONファイルの内容をDialogueオブジェクトに変換する
            dialogue = JsonUtility.FromJson<Dialogue>(json.text);
            
            // 結果を表示
            Debug.Log($"JSONからデータを読み取りました: {dialogue.title}");
            foreach(var line in dialogue.lines)
                Debug.Log($"- {line}");
        }
    }
}