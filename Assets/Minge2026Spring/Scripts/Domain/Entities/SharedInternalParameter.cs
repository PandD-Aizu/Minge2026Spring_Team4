using System.Runtime.InteropServices;

namespace Minge2026Spring.Scripts.Domain.Entities
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SharedInternalParameter
    {
        public int CharacterId;
        public int Morale;
    }
}