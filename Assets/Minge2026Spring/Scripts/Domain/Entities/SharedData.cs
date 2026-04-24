using System.Runtime.InteropServices;

namespace Minge2026Spring.Scripts.Domain.Entities
{
    [StructLayout(LayoutKind.Sequential)]    
    public struct SharedData
    {
        public SharedInternalParameter character1;
        public SharedInternalParameter character2;
        public SharedInternalParameter character3;
        public SharedInternalParameter character4;
    }
}