using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Domain.ValueObject;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class SceneTransitionProvider : ISceneTransitionProvider
    {
        public async UniTaskVoid LoadSceneAsync(SceneLabel label)
        {
            var handle = Addressables.LoadSceneAsync(label.Value);
            try
            {
                await handle.Task.AsUniTask();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneTransitionProvider] Failed to load scene '{label.Value}'.\n{e}");
            }
        }
    }
}