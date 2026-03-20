using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class ApplicationStopUseCase
    {
        private readonly IApplicationStopProvider _applicationStopProvider;
        
        public ApplicationStopUseCase(IApplicationStopProvider applicationStopProvider)
        {
            _applicationStopProvider = applicationStopProvider;
        }

        public void StopApplication()
        {
            _applicationStopProvider.StopApplication();
        }
    }
}