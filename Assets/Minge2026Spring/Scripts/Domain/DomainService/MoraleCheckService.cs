using System.Linq;
using Minge2026Spring.Scripts.Domain.Entities;
using Minge2026Spring.Scripts.Domain.ValueObjects;

namespace Minge2026Spring.Scripts.Domain.DomainService
{
    public class MoraleCheckService
    {
        /// <summary>
        /// 指定したキャラクターの士気度を士気ラベルに変換して返す
        /// </summary>
        /// <param name="characterId">キャラクターの名前</param>
        /// <param name="collection">キャラクターたちの内部パラメータ</param>
        /// <returns></returns>
        public MoraleLabel GetMoraleLabel(string characterId, InternalParameterCollection collection)
        {
            var param = collection.GetParameter(characterId);
            if (param == null)
                return MoraleLabel.MORALE_LOW;

            return MoraleRange.GetLabel(param.Morale);
        }

        /// <summary>
        /// 全てのキャラクターの士気が指定した士気ラベル以上かをチェック
        /// </summary>
        /// <param name="collection">キャラクターの内部パラメータ</param>
        /// <param name="label">士気ラベル</param>
        /// <returns>すべて指定以上: true</returns>
        public bool IsAllMoraleAbove(InternalParameterCollection collection, MoraleLabel label)
        {
            return collection.Characters
                .Select(c => GetMoraleLabel(c.Key, collection))
                .All(moraleLabel => moraleLabel >= label);
        }

        /// <summary>
        /// 指定した士気ラベル以上のキャラクターが1人でもいるかをチェック
        /// </summary>
        /// <param name="collection">キャラクターの内部パラメータ</param>
        /// <param name="label">士気ラベル</param>
        /// <returns>1人でもいる: true</returns>
        public bool AnyMoraleIs(InternalParameterCollection collection, MoraleLabel label)
        {
            return collection.Characters
                .Select(c => GetMoraleLabel(c.Key, collection))
                .Any(moraleLabel => moraleLabel == label);
        }
    }
}