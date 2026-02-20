using System.Text;
using Codice.CM.Common.Merge;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Infrastructure.DTOs;
using UnityEngine;
using UnityEngine.Networking;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class LLMProvider : ILLMProvider
    {
        private const string URL = "http://localhost:11434/api/generate";
        private const string MODEL_NAME = "gemma3:4b";
        private const string SYSTEM_PROMPT = "あなたは、すべての母です。全力でよしよししてください。";
        
        /// <summary>
        /// LLMにユーザーの入力を送信し、応答を受け取るメソッド
        /// </summary>
        /// <param name="userInput">ユーザー入力</param>
        /// <returns>LLMの応答</returns>
        public string SendRequest(string userInput)
        {
            return GenerateRequestAsync(userInput)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// ユーザーの入力をLLMに送信し、応答を受け取る非同期メソッド
        /// </summary>
        /// <param name="userInput">ユーザー入力</param>
        /// <returns></returns>
        private async UniTask<string> GenerateRequestAsync(string userInput)
        {
            // LLMへのリクエストデータを作成
            var requestData = new LLMRequest()
            {
                model = MODEL_NAME,
                systemPrompt = SYSTEM_PROMPT,
                userPrompt =  userInput,
                stream = false
            };
            var json = JsonUtility.ToJson(requestData);

            // REST APIにリクエストを送るためのUnityWebRequestのセットアップ
            using var request = new UnityWebRequest(URL, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            // 送信して返答を待機
            await request.SendWebRequest();
            
            // エラーチェック
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"LLMProvider Error: {request.error}");
                return null;
            }
            
            // 返答を処理
            var responseJson = request.downloadHandler.text;
            var responseData = JsonUtility.FromJson<LLMResponse>(responseJson);
            
            return responseData.response;
        } 
    }
}