using Minge2026Spring.Scripts.Application.DTOs;

namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface IGameSaveRepository
    {
        /// <summary>
        /// セーブデータを読み込む
        /// </summary>
        /// <returns>正常に読み込めたセーブデータ、存在しない場合はnull</returns>
        GameSaveData Load();

        /// <summary>
        /// セーブデータを保存する
        /// </summary>
        /// <param name="saveData">保存するゲーム進行データ</param>
        void Save(GameSaveData saveData);

        /// <summary>
        /// セーブデータを削除する
        /// </summary>
        void Delete();
    }
}
