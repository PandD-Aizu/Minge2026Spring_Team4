using System;

namespace Minge2026Spring.Scripts.Infrastructure.DTOs
{
    /// <summary>
    /// LLMへのリクエスト
    /// </summary>
    [Serializable]
    public class LLMRequest
    {
        public string model;  // 使用するLLMのモデル名
        public string system; // LLMに対する指示など
        public string prompt; // ユーザーからの入力
        public bool stream;   // ストリーミング応答(逐次的に応答を受け取るかどうか)
    }
}