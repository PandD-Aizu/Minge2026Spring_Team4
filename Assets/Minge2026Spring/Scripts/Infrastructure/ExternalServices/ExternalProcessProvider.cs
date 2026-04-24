using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class ExternalProcessProvider : IDisposable, IExternalProcessProvider
    {
        private Process _process = null;
        private const int SwShowNormal = 1;
        
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr ShellExecute(
            IntPtr hwnd,
            string lpOperation,
            string lpFile,
            string lpParameters,
            string lpDirectory,
            int nShowCmd
        );
        
        /// <inheritdoc/>
        public void StartProcess(string processPath)
        {
            // プロセスパスが無効な場合は、エラーを出力してプロセスを終了する
            if (string.IsNullOrEmpty(processPath))
            {
                UnityEngine.Debug.LogError($"Invalid process path: {processPath}");
                return;
            }

            var normalizedPath = Path.GetFullPath(processPath.Trim());
            if (!File.Exists(normalizedPath))
            {
                UnityEngine.Debug.LogError($"[ExternalProcessProvider] Executable not found.\noriginalPath = {processPath}\nnormalizedPath = {normalizedPath}");
                return;
            }

            // プロセスが既に起動している場合は、エラーを出力してプロセスを終了する
            if (_process is { HasExited: false })
            {
                UnityEngine.Debug.LogError($"Process is already running: {_process.ProcessName}\nKilling the process...");
                _process.Kill();
            }
            
            var workingDirectory = Path.GetDirectoryName(normalizedPath) ?? Environment.CurrentDirectory;
            if (TryStartNativeShellExecute(normalizedPath, workingDirectory))
            {
                _process = null;
                return;
            }

            if (TryStartDirect(normalizedPath, workingDirectory, out var directProcess))
            {
                _process = directProcess;
                return;
            }

            if (TryStartShell(normalizedPath, workingDirectory, out var shellProcess))
            {
                _process = shellProcess;
                return;
            }

            if (TryStartViaCmdStart(normalizedPath, workingDirectory, out var cmdProcess))
            {
                _process = cmdProcess;
                return;
            }

            UnityEngine.Debug.LogError(
                $"[ExternalProcessProvider] All launch strategies failed.\n" +
                $"originalPath = {processPath}\n" +
                $"normalizedPath = {normalizedPath}\n" +
                $"workingDirectory = {workingDirectory}");
        }

        private static bool TryStartNativeShellExecute(string path, string workingDirectory)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return false;

            try
            {
                var result = ShellExecute(IntPtr.Zero, "open", path, null, workingDirectory, SwShowNormal);
                var code = result.ToInt64();
                if (code > 32)
                {
                    UnityEngine.Debug.Log($"[ExternalProcessProvider] Process started. strategy = native-shell, fileName = {path}");
                    return true;
                }

                UnityEngine.Debug.LogError(
                    $"[ExternalProcessProvider] Native shell launch failed.\n" +
                    $"strategy = native-shell\n" +
                    $"shellExecuteResult = {code}\n" +
                    $"lastWin32Error = {Marshal.GetLastWin32Error()}\n" +
                    $"fileName = {path}\n" +
                    $"workingDirectory = {workingDirectory}");
                return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(
                    $"[ExternalProcessProvider] Native shell launch threw exception.\n" +
                    $"strategy = native-shell\n" +
                    $"fileName = {path}\n" +
                    $"workingDirectory = {workingDirectory}\n" +
                    $"exception = {e}");
                return false;
            }
        }

        private static bool TryStartDirect(string path, string workingDirectory, out Process process)
        {
            var app = new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false
            };

            return TryStartWithStrategy("direct", app, out process);
        }

        private static bool TryStartShell(string path, string workingDirectory, out Process process)
        {
            var app = new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true,
                Verb = "open"
            };

            return TryStartWithStrategy("shell", app, out process);
        }

        private static bool TryStartViaCmdStart(string path, string workingDirectory, out Process process)
        {
            var app = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{path}\"",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            return TryStartWithStrategy("cmd-start", app, out process);
        }

        private static bool TryStartWithStrategy(string strategy, ProcessStartInfo startInfo, out Process process)
        {
            process = null;
            try
            {
                process = Process.Start(startInfo);
                if (process is null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ExternalProcessProvider] Process.Start returned null. strategy = {strategy}, fileName = {startInfo.FileName}");
                    return false;
                }

                UnityEngine.Debug.Log(
                    $"[ExternalProcessProvider] Process started. strategy = {strategy}, fileName = {startInfo.FileName}");
                return true;
            }
            catch (Exception e)
            {
                var nativeErrorCode = (e is Win32Exception win32) ? win32.NativeErrorCode.ToString() : "n/a";
                UnityEngine.Debug.LogError(
                    $"[ExternalProcessProvider] Launch attempt failed.\n" +
                    $"strategy = {strategy}\n" +
                    $"message = {e.Message}\n" +
                    $"type = {e.GetType().Name}\n" +
                    $"nativeErrorCode = {nativeErrorCode}\n" +
                    $"fileName = {startInfo.FileName}\n" +
                    $"arguments = {startInfo.Arguments}\n" +
                    $"workingDirectory = {startInfo.WorkingDirectory}\n" +
                    $"useShellExecute = {startInfo.UseShellExecute}");
                return false;
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

        /// <inheritdoc/>
        public bool IsProcessRunning()
        {
            return _process is not null && _process.HasExited;
        }

        public void Dispose()
        {
            _process?.Dispose();
        }
    }
}