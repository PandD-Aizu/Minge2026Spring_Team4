using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Domain.ValueObject;

namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface ISceneTransitionProvider
    {
        public UniTaskVoid LoadSceneAsync(SceneLabel label);
    }
}