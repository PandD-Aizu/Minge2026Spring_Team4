using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Infrastructure.DTOs;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class SharedMemoryService : ISharedMemoryService, IDisposable
    {
        private const string MAP_NAME = "Minge2026Spring_SharedMemory";
        private const string MUTEX_NAME = "Minge2026Spring_MainProcessMutex";
        private static readonly int SIZE = Marshal.SizeOf<SharedData>();

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private Mutex _mutex;
        private bool _disposed = false;

        /// <summary>
        /// 共有メモリとミューテックスを初期化する
        /// </summary>
        public void Init()
        {
            try
            {
                _mmf = MemoryMappedFile.CreateOrOpen(MAP_NAME, SIZE);
                _accessor = _mmf.CreateViewAccessor();
                _mutex = new Mutex(false, MUTEX_NAME);

                UnityEngine.Application.quitting += Dispose;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[SharedMemoryService] Failed to initialize shared memory: {e}");
            }
        }

        /// <summary>
        /// 共有メモリにSharedData構造体のデータを書き込む
        /// </summary>
        /// <param name="data">書き込むデータ</param>
        public void WriteData(SharedData data)
        {
            if (_disposed || _mutex == null)
            {
                UnityEngine.Debug.LogWarning("[SharedMemoryService] Not initialized or already disposed.");
                return;
            }
            
            if (!_mutex.WaitOne(millisecondsTimeout: 100))
            {
                UnityEngine.Debug.LogWarning("[SharedMemoryService] Failed to acquire mutex for writing data.");
                return;
            }

            try
            {
                _accessor.Write(0, ref data);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 共有メモリのデータを読み取り、SharedData構造体として返す
        /// </summary>
        /// <returns>共有メモリのデータ</returns>
        public SharedData ReadData()
        {
            if (_disposed || _mutex == null)
            {
                UnityEngine.Debug.LogWarning("[SharedMemoryService] Not initialized or already disposed.");
                return default;
            }
            
            if (!_mutex.WaitOne(millisecondsTimeout: 100))
            {
                UnityEngine.Debug.LogWarning("[SharedMemoryService] Failed to acquire mutex for reading data.");
                return default;
            }

            try
            {
                _accessor.Read(0, out SharedData data);
                return data;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            UnityEngine.Application.quitting -= Dispose;
            _accessor?.Dispose();
            _mmf?.Dispose();
            _mutex?.Dispose();
        }
    }
}