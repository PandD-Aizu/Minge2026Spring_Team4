using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class ApplicationStopProvider : IApplicationStopProvider
    {
        public void StopApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}