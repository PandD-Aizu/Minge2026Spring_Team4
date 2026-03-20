using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Domain.Entities;

namespace Minge2026Spring.Scripts.Domain.Interface
{
    public interface IInternalParameterRepository
    {
        public UniTask<InternalParameterCollection> LoadAsync();
        public void SaveAsync(InternalParameterCollection parameter);
        public bool IsSaveDataExists();
    }
}