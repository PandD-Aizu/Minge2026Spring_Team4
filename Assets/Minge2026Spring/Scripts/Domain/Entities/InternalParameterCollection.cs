using System;
using System.Collections.Generic;

namespace Minge2026Spring.Scripts.Domain.Entities
{
    [Serializable]
    public class InternalParameterCollection
    {
        public Dictionary<string, InternalParameter> Characters = new Dictionary<string, InternalParameter>();

        /// <summary>
        /// キャラクター名に対応するパラメータを取得
        /// 存在しない場合は新規に作成して返す
        /// </summary>
        /// <param name="characterId">キャラクターの名前</param>
        /// <returns>キャラクター名に対応する内部パラメータ</returns>
        public InternalParameter GetParameter(string characterId)
        {
            return Characters.GetValueOrDefault(characterId);
        }

        /// <summary>
        /// キャラクター名に対応するパラメータを設定
        /// </summary>
        /// <param name="characterId">キャラクターの名前</param>
        /// <param name="parameter">設定する内部パラメータ</param>
        public void SetParameter(string characterId, InternalParameter parameter)
        {
            Characters[characterId] = parameter;
        }
    }
}