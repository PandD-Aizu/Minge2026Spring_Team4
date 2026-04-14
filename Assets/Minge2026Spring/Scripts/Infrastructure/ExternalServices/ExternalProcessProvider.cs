using System;
using System.Diagnostics;
using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class ExternalProcessProvider : IDisposable, IExternalProcessProvider
    {
        private Process _process = null;
        
        /// <inheritdoc/>
        public void StartProcess(string processPath)
        {
            // プロセスパスが無効な場合は、エラーを出力してプロセスを終了する
            if (string.IsNullOrEmpty(processPath))
            {
                UnityEngine.Debug.LogError($"Invalid process path: {processPath}");
                return;
            }

            // プロセスが既に起動している場合は、エラーを出力してプロセスを終了する
            if (_process is { HasExited: false })
            {
                UnityEngine.Debug.LogError($"Process is already running: {_process.ProcessName}\nKilling the process...");
                _process.Kill();
            }
            
            var app = new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = true
            };

            try
            {
                _process = Process.Start(app);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to start process: {e.Message}\nprocessPath = {processPath}");
            }
        }

        /// <inheritdoc/>
        public void StopProcess()
        {
            if (_process is null)
            {
                UnityEngine.Debug.LogError("Process is null");
                return;
            }

            if (_process.HasExited)
            {
                UnityEngine.Debug.Log("Process has already exited");
                return;
            }
            
            _process.Kill();
        }

        public void Dispose()
        {
            _process?.Dispose();
            _process?.Dispose();
        }
    }
}