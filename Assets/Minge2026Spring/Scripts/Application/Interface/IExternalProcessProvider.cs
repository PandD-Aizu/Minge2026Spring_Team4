namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface IExternalProcessProvider
    {
        /// <summary>
        /// 指定したプロセスを起動する
        /// </summary>
        /// <param name="processPath">実行ファイルまでのパス</param>
        void StartProcess(string processPath);
        
        /// <summary>
        /// 指定したプロセスを終了する
        /// </summary>
        void StopProcess();
    }
}