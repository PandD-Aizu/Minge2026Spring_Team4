using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class GameStarterUseCase
    {
        private readonly IExternalProcessProvider _externalProcessProvider;

        public GameStarterUseCase(IExternalProcessProvider externalProcessProvider)
        {
            _externalProcessProvider = externalProcessProvider;
        }

        public void StartGame(string processPath)
        {
            _externalProcessProvider.StartProcess(processPath);
        }

        public void StopGame()
        {
            _externalProcessProvider.StopProcess();
        }
    }
}