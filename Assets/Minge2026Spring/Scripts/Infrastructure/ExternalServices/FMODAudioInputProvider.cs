using System;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using Minge2026Spring.Scripts.Application.Interface;

namespace Minge2026Spring.Scripts.Infrastructure.ExternalServices
{
    public class FMODAudioInputProvider : IFMODAudioInputProvider, IDisposable
    {
        private FMOD.System coreSystem;
        private FMOD.Sound micSound;
        private FMOD.Channel micChannel;

        private FMOD.CREATESOUNDEXINFO exinfo;
        private const int SAMPLE_RATE = 48000;
        private const int CHANNELS = 1;

        private const int RECORD_DEVICE_INDEX = 0;

        public FMODAudioInputProvider()
        {
            // FMOD Core Systemを取得
            coreSystem = RuntimeManager.CoreSystem;
            
            // 接続されているマイクデバイスを取得
            int numDrivers;
            int numConnected;
            coreSystem.getRecordNumDrivers(out numDrivers, out numConnected);
            if (numConnected == 0)
            {
                UnityEngine.Debug.Log("No recording devices found.");
                return;
            }
            
            // 録音用のSoundオブジェクトを作成
            exinfo = new FMOD.CREATESOUNDEXINFO();
            exinfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
            exinfo.numchannels = CHANNELS;
            exinfo.format = FMOD.SOUND_FORMAT.PCMFLOAT;
            exinfo.defaultfrequency = SAMPLE_RATE;
            exinfo.length = (uint)(SAMPLE_RATE * CHANNELS * sizeof(float));
        }
        
        /// <summary>
        /// マイクからの音声を録音してSoundオブジェクトとして返す
        /// </summary>
        /// <returns>録音したSoundオブジェクト</returns>
        public Sound RecordAudio()
        {
            // 空のサウンドオブジェクトを作成
            FMOD.RESULT result = coreSystem.createSound(
                "",
                FMOD.MODE.OPENUSER | FMOD.MODE.LOOP_NORMAL | FMOD.MODE.OPENRAW,
                ref exinfo,
                out micSound
            );

            // エラーチェック
            if (result != FMOD.RESULT.OK)
            {
                UnityEngine.Debug.LogError($"Sound creation failed: {result}");
                return new Sound();
            }
            
            // 録音を開始する
            result = coreSystem.recordStart(RECORD_DEVICE_INDEX, micSound, true);
            if (result != FMOD.RESULT.OK)
            {
                UnityEngine.Debug.LogError($"Failed to start recording: {result}");
                return new Sound();
            }
            
            // 録音したデータを返す
            return micSound;
        }

        public void Dispose()
        {
            if (coreSystem.recordStop(RECORD_DEVICE_INDEX) == FMOD.RESULT.OK)
            {
                if (micSound.hasHandle())
                {
                    micSound.release();
                }
            }
        }
    }
}