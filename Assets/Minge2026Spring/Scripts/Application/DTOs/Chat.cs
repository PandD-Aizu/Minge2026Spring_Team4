using System;
using System.Collections.Generic;

namespace Minge2026Spring.Scripts.Application.DTOs
{
    public static class CharacterMoraleKeys
    {
        public const string CharacterA = "Got";
        public const string CharacterB = "Ryuta";
        public const string CharacterC = "Milu";
        public const string CharacterD = "kashiwa";
    }

    public readonly struct CharacterMoraleDelta
    {
        public string CharacterId { get; }
        public int Delta { get; }

        public CharacterMoraleDelta(string characterId, int delta)
        {
            CharacterId = characterId;
            Delta = delta;
        }
    }

    [Serializable]
    public class Chapter
    {
        public ChapterBlock[] blocks; // シナリオブロックのリスト
    }
    
    public enum ChapterNodeType
    {
        Dialogue,
        Choice,
        LLM
    }

    [Serializable]
    public class ChapterBlock
    {
        public string blockId;           // このブロック固有のID
        public ChapterNodeType nodeType; // ノードの種類
        public float nodePosX;           // ノードのX座標
        public float nodePosY;           // ノードのY座標
        
        public Dialogue[] dialogues; // このブロックで表示される会話群
        
        public Choice[] choices;     // このブロックで表示される選択肢群
        public string nextBlockId;   // 選択肢がない場合に移動する次のブロックのID

        public FreeChat[] freeChats;

        public float waitingTime;    // 次の会話ブロックを表示するまで待機する時間(s)
    }

    [Serializable]
    public struct Dialogue
    {
        public string iconId;     // 読み込むアイコン画像のID
        
        public string speaker;    // 発言者の名前
        public string message;    // 発言内容

        // FMODのEventReferenceを章グラフエディターで選択し、JSONにはイベントパスとして保存する。
        public string voiceEventPath;

        // 音声再生終了後、次のセリフへ進むまでの追加待機時間(s)
        public float waitingTime;
    }

    [Serializable]
    public struct Choice
    {
        public string choiceText;  // 選択肢のテキスト
        public string nextBlockId; // 選択肢を選んだ場合の次のブロックのID

        public int characterAMoraleDelta; // CharacterAの士気変化量
        public int characterBMoraleDelta; // CharacterBの士気変化量
        public int characterCMoraleDelta; // CharacterCの士気変化量
        public int characterDMoraleDelta; // CharacterDの士気変化量

        public IEnumerable<CharacterMoraleDelta> GetMoraleDeltas()
        {
            yield return new CharacterMoraleDelta(CharacterMoraleKeys.CharacterA, characterAMoraleDelta);
            yield return new CharacterMoraleDelta(CharacterMoraleKeys.CharacterB, characterBMoraleDelta);
            yield return new CharacterMoraleDelta(CharacterMoraleKeys.CharacterC, characterCMoraleDelta);
            yield return new CharacterMoraleDelta(CharacterMoraleKeys.CharacterD, characterDMoraleDelta);
        }
    }

    [Serializable]
    public struct FreeChat
    {
        public float waitingTime;    // 音声入力の待機時間
        public float recordDuration; // 音声入力できる時間
    }
}
