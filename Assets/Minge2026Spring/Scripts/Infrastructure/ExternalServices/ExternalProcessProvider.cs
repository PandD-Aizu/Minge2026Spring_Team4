using System;
using System.IO;
using System.Runtime.InteropServices;
using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    /// <summary>
    /// Windows上で外部ゲームを起動し、そのプロセスと子孫プロセスを管理する。
    /// Job Objectを使うことで、親ゲームの異常終了時にもOSが関連プロセスを回収できる。
    /// </summary>
    public sealed class ExternalProcessProvider : IDisposable, IExternalProcessProvider
    {
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

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
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
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(
            IntPtr hJob,
            int jobObjectInfoClass,
            ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo,
            uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateJobObject(IntPtr hJob, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint CREATE_SUSPENDED = 0x00000004;
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
        private const int JobObjectExtendedLimitInformation = 9;
        private const uint WAIT_OBJECT_0 = 0x00000000;
        private const uint WAIT_TIMEOUT = 0x00000102;
        private const uint WAIT_FAILED = 0xFFFFFFFF;
        private const uint StopWaitMilliseconds = 5000;

        private IntPtr _processHandle;
        private IntPtr _jobHandle;
        private bool _disposed;

        public ExternalProcessProvider()
        {
            UnityEngine.Application.quitting += StopProcess;
        }

        public bool StartProcess(string processPath)
        {
            if (_disposed || string.IsNullOrWhiteSpace(processPath))
                return false;

#if !UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN
            UnityEngine.Debug.LogError("[ExternalProcessProvider] Windows only.");
            return false;
#else
            string normalizedPath;
            try
            {
                normalizedPath = Path.GetFullPath(processPath.Trim());
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException)
            {
                UnityEngine.Debug.LogError($"[ExternalProcessProvider] Invalid process path: {exception.Message}");
                return false;
            }

            if (!File.Exists(normalizedPath))
            {
                UnityEngine.Debug.LogError($"[ExternalProcessProvider] File not found: {normalizedPath}");
                return false;
            }

            var workingDirectory = Path.GetDirectoryName(normalizedPath);
            if (string.IsNullOrEmpty(workingDirectory))
            {
                UnityEngine.Debug.LogError($"[ExternalProcessProvider] Working directory could not be determined: {normalizedPath}");
                return false;
            }

            StopProcess();

            var jobHandle = CreateJobObject(IntPtr.Zero, null);
            if (jobHandle == IntPtr.Zero)
            {
                LogLastWin32Error("CreateJobObject");
                return false;
            }

            var jobLimits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            jobLimits.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
            if (!SetInformationJobObject(
                    jobHandle,
                    JobObjectExtendedLimitInformation,
                    ref jobLimits,
                    (uint)Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION))))
            {
                LogLastWin32Error("SetInformationJobObject");
                CloseHandle(jobHandle);
                return false;
            }

            var startupInfo = new STARTUPINFO { cb = Marshal.SizeOf(typeof(STARTUPINFO)) };
            PROCESS_INFORMATION processInfo;
            var success = CreateProcess(
                normalizedPath,
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                CREATE_SUSPENDED,
                IntPtr.Zero,
                workingDirectory,
                ref startupInfo,
                out processInfo);

            if (!success)
            {
                LogLastWin32Error("CreateProcess");
                CloseHandle(jobHandle);
                return false;
            }

            var assigned = AssignProcessToJobObject(jobHandle, processInfo.hProcess);
            if (!assigned)
            {
                LogLastWin32Error("AssignProcessToJobObject");
                TerminateProcess(processInfo.hProcess, 1);
                WaitForSingleObject(processInfo.hProcess, StopWaitMilliseconds);
                CloseHandle(processInfo.hThread);
                CloseHandle(processInfo.hProcess);
                CloseHandle(jobHandle);
                return false;
            }

            if (ResumeThread(processInfo.hThread) == 0xFFFFFFFF)
            {
                LogLastWin32Error("ResumeThread");
                TerminateJobObject(jobHandle, 1);
                WaitForSingleObject(processInfo.hProcess, StopWaitMilliseconds);
                CloseHandle(processInfo.hThread);
                CloseHandle(processInfo.hProcess);
                CloseHandle(jobHandle);
                return false;
            }

            CloseHandle(processInfo.hThread);
            _processHandle = processInfo.hProcess;
            _jobHandle = jobHandle;
            UnityEngine.Debug.Log($"[ExternalProcessProvider] Process started: {normalizedPath}");
            return true;
#endif
        }

        public void StopProcess()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_processHandle == IntPtr.Zero && _jobHandle == IntPtr.Zero)
                return;

            var processHandle = _processHandle;
            var jobHandle = _jobHandle;
            _processHandle = IntPtr.Zero;
            _jobHandle = IntPtr.Zero;

            if (jobHandle != IntPtr.Zero)
                TerminateJobObject(jobHandle, 0);
            else if (processHandle != IntPtr.Zero)
                TerminateProcess(processHandle, 0);

            if (processHandle != IntPtr.Zero)
            {
                var waitResult = WaitForSingleObject(processHandle, StopWaitMilliseconds);
                if (waitResult == WAIT_TIMEOUT)
                    UnityEngine.Debug.LogWarning("[ExternalProcessProvider] Process did not exit within the timeout.");
                else if (waitResult == WAIT_FAILED)
                    LogLastWin32Error("WaitForSingleObject");

                CloseHandle(processHandle);
            }

            if (jobHandle != IntPtr.Zero)
                CloseHandle(jobHandle);
#endif
        }

        public bool IsProcessRunning()
        {
#if !UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN
            return false;
#else
            if (_processHandle == IntPtr.Zero)
                return false;

            var waitResult = WaitForSingleObject(_processHandle, 0);
            if (waitResult == WAIT_TIMEOUT)
                return true;

            if (waitResult == WAIT_OBJECT_0)
            {
                ReleaseFinishedProcess();
                return false;
            }

            // APIエラーを終了扱いにすると、誤って結果画面へ遷移するため、
            // ハンドルが有効な間は次回Tickで再確認する。
            LogLastWin32Error("WaitForSingleObject");
            return true;
#endif
        }

        private void ReleaseFinishedProcess()
        {
            if (_processHandle != IntPtr.Zero)
            {
                CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
            }

            // Jobを閉じると、万一残っている子孫プロセスも回収される。
            if (_jobHandle != IntPtr.Zero)
            {
                CloseHandle(_jobHandle);
                _jobHandle = IntPtr.Zero;
            }
        }

        private static void LogLastWin32Error(string operation)
        {
            UnityEngine.Debug.LogError($"[ExternalProcessProvider] {operation} failed. ErrorCode: {Marshal.GetLastWin32Error()}");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            UnityEngine.Application.quitting -= StopProcess;
            StopProcess();
        }
    }
}
