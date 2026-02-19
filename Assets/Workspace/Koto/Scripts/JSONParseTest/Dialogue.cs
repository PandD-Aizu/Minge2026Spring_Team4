using System;

namespace Workspace.Koto.Scripts.JSONLoadTest
{
    /// <summary>
    /// JSONファイルから読み取るデータ構造を定義するクラス
    /// </summary>
    [Serializable]
    public class Dialogue
    {
        public string title;
        public string[] lines;
    }
}