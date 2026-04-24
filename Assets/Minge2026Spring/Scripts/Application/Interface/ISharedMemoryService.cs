using Minge2026Spring.Scripts.Domain.Entities;

namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface ISharedMemoryService
    {
        public void Init();
        public void WriteData(SharedData data);
        public SharedData ReadData();
    }
}