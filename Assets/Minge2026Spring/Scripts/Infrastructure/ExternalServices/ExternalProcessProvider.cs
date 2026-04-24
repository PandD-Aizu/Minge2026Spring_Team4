using System;
using System.IO;
using System.Runtime.InteropServices;
using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class ExternalProcessProvider : IDisposable, IExternalProcessProvider
    {
        // === Windows OSネイティブAPI (kernel32.dll) の定義 ===
        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        private const uint STILL_ACTIVE = 259;

        // Windowsから直接もらうプロセスの管理番号
        private IntPtr _processHandle = IntPtr.Zero;

        public bool StartProcess(string processPath)
        {
            if (string.IsNullOrEmpty(processPath)) return false;

            var normalizedPath = Path.GetFullPath(processPath.Trim());
            if (!File.Exists(normalizedPath))
            {
                UnityEngine.Debug.LogError($"[ExternalProcessProvider] File not found: {normalizedPath}");
                return false;
            }

            var workingDirectory = Path.GetDirectoryName(normalizedPath);

            // 起動前に前のプロセスが残っていれば確実に閉じる
            StopProcess();

            var startupInfo = new STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(startupInfo);

            // C#のバグを回避し、Windowsの根幹APIで直接起動する
            bool success = CreateProcess(
                normalizedPath,
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                0,
                IntPtr.Zero,
                workingDirectory,
                ref startupInfo,
                out PROCESS_INFORMATION processInfo);

            if (success)
            {
                UnityEngine.Debug.Log("[ExternalProcessProvider] CreateProcess 成功！");
                _processHandle = processInfo.hProcess; // 絶対に見失わないハンドルを保持
                
                // スレッドハンドルは今回不要なので閉じてメモリリークを防ぐ
                CloseHandle(processInfo.hThread);
                return true;
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                UnityEngine.Debug.LogError($"[ExternalProcessProvider] CreateProcess 失敗。ErrorCode: {errorCode}");
                return false;
            }
        }

        public void StopProcess()
        {
            if (_processHandle != IntPtr.Zero)
            {
                TerminateProcess(_processHandle, 0);
                CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
            }
        }

        public bool IsProcessRunning()
        {
            if (_processHandle == IntPtr.Zero) return false;

            // WindowsOSにプロセスの状態を直接問い合わせる
            if (GetExitCodeProcess(_processHandle, out uint exitCode))
            {
                if (exitCode == STILL_ACTIVE)
                {
                    return true; // まだ元気に実行中
                }
                else
                {
                    UnityEngine.Debug.Log($"[ExternalProcessProvider] プロセスが終了しました。ExitCode: {exitCode}");
                    CloseHandle(_processHandle);
                    _processHandle = IntPtr.Zero;
                    return false; // 終了した
                }
            }
            
            return false;
        }

        public void UpdateProcessHandle()
        {
            
        }

        public void Dispose()
        {
            StopProcess();
        }
    }
}