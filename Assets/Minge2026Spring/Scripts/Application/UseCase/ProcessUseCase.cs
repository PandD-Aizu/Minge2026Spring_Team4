using System.IO;
using Minge2026Spring.Scripts.Application.Interface;
using R3;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class ProcessUseCase
    {
        private readonly IExternalProcessProvider _externalProcessProvider;

        public ReadOnlyReactiveProperty<bool> IsProcessRunning => _isProcessRunning.ToReadOnlyReactiveProperty();
        private readonly ReactiveProperty<bool> _isProcessRunning = new (false);

        public ProcessUseCase(IExternalProcessProvider externalProcessProvider)
        {
            _externalProcessProvider = externalProcessProvider;
        }

        /// <summary>
        /// プロセスを起動する
        /// </summary>
        /// <param name="startProcess">プロセス名</param>
        public void StartProcess(string startProcess)
        {
            var combinedPass = Path.Combine(UnityEngine.Application.streamingAssetsPath, startProcess);
            
            _externalProcessProvider.StartProcess(combinedPass);
            _isProcessRunning.Value = true;
        }

        /// <summary>
        /// プロセスを停止する
        /// </summary>
        public void StopProcess()
        {
            _externalProcessProvider.StopProcess();
            _isProcessRunning.Value = false;
        }

        /// <summary>
        /// プロセスが起動しているかどうかをチェックする
        /// </summary>
        public void CheckProcessIsRunning()
        {
            _isProcessRunning.Value = !_externalProcessProvider.IsProcessRunning();
        }
    }
}