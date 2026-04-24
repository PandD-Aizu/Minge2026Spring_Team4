namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface IExternalProcessProvider
    {
        /// <summary>
        /// 指定したプロセスを起動する
        /// </summary>
        /// <param name="processPath">実行ファイルまでのパス</param>
        bool StartProcess(string processPath);
        
        /// <summary>
        /// 指定したプロセスを終了する
        /// </summary>
        void StopProcess();

        /// <summary>
        /// プロセスが起動しているかどうかチェック
        /// </summary>
        /// <returns>起動している: false</returns>
        bool IsProcessRunning();

        /// <summary>
        /// ビルド版で null になったプロセスハンドルを復旧する
        /// </summary>
        void UpdateProcessHandle();
    }
}