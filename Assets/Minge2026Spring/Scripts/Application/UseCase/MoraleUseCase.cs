using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Domain.DomainService;
using Minge2026Spring.Scripts.Domain.Entities;
using Minge2026Spring.Scripts.Domain.Interface;
using Minge2026Spring.Scripts.Domain.ValueObjects;
using UnityEngine;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class MoraleUseCase
    {
        public event System.Action MoraleChanged;

        private readonly IInternalParameterRepository _repository;
        private readonly ISharedMemoryService _sharedMemory;
        private readonly MoraleCheckService _moraleCheckService;

        private InternalParameterCollection _cache;

        public MoraleUseCase(
            IInternalParameterRepository repository,
            ISharedMemoryService sharedMemory,
            MoraleCheckService moraleCheckService)
        {
            _repository = repository;
            _sharedMemory = sharedMemory;
            _moraleCheckService = moraleCheckService;
        }

        /// <summary>
        /// ゲーム開始時の処理
        /// </summary>
        public async UniTaskVoid OnGameStart()
        {
            _sharedMemory.Init();
            _cache = await _repository.LoadAsync();
            EnsureDefaultCharacters();
            _sharedMemory.WriteData(ToSharedData(_cache));
            MoraleChanged?.Invoke();
        }

        /// <summary>
        /// セーブ時の処理
        /// 現在の士気度を保存する
        /// </summary>
        public void OnSave()
        {
            if (_cache == null)
            {
                UnityEngine.Debug.LogWarning("[MoraleUseCase] No data to save.");
                return;
            }

            _repository.SaveAsync(_cache);
        }

        /// <summary>
        /// 全キャラクターのやる気度を初期化する。
        /// </summary>
        public void ResetForRestart()
        {
            _cache = new InternalParameterCollection();
            EnsureDefaultCharacters();
            _sharedMemory.WriteData(ToSharedData(_cache));
            _repository.SaveAsync(_cache);
            MoraleChanged?.Invoke();
        }

        /// <summary>
        /// 現在の士気度をDTOとして取得する
        /// </summary>
        /// <returns></returns>
        public MoraleDto GetMoraleDto()
        {
            if (_cache == null)
            {
                _cache = new InternalParameterCollection();
                EnsureDefaultCharacters();
            }

            var dto = new MoraleDto();
            foreach(var character in _cache.Characters)
                dto.MoraleMap[character.Key] = character.Value.Morale;
            return dto;
        }

        public bool TryGetMoraleDto(out MoraleDto moraleDto)
        {
            if (_cache == null)
            {
                moraleDto = null;
                return false;
            }

            moraleDto = GetMoraleDto();
            return true;
        }

        /// <summary>
        /// 士気度を加算する
        /// </summary>
        /// <param name="characterId">キャラクター名</param>
        /// <param name="amount">加算量</param>
        public void AddMorale(string characterId, int amount)
        {
            if (_cache == null)
            {
                _cache = new InternalParameterCollection();
                EnsureDefaultCharacters();
            }

            var parameter = _cache.GetParameter(characterId);
            if (parameter == null)
            {
                parameter = new InternalParameter();
                _cache.SetParameter(characterId, parameter);
            }
            
            parameter.Morale += amount;
            _sharedMemory.WriteData(ToSharedData(_cache));
            MoraleChanged?.Invoke();
        }
        
        /// <summary>
        /// 全員の士気レベルを取得する
        /// </summary>
        /// <param name="characterId">キャラクターの名前</param>
        /// <returns>士気度ラベル</returns>
        public MoraleLabel GetMoraleLevel(string characterId)
        {
            return _moraleCheckService.GetMoraleLabel(characterId, _cache);
        }
        
        /// <summary>
        /// 全員が指定レベル以上か判定する
        /// </summary>
        /// <param name="label">士気度ラベル</param>
        /// <returns>指定レベル以上: true</returns>
        public bool IsAllMoraleAbove(MoraleLabel label)
        {
            return _moraleCheckService.IsAllMoraleAbove(_cache, label);
        }

        /// <summary>
        /// Dto -> SharedDataに変換する
        /// </summary>
        /// <param name="collection">全キャラクターの内部データ</param>
        /// <returns>共有メモリのデータ定義</returns>
        private SharedData ToSharedData(InternalParameterCollection collection)
        {
            return new SharedData
            {
                character1 = ToSharedParam(CharacterId.CharacterA, collection.GetParameter("CharacterA")),
                character2 = ToSharedParam(CharacterId.CharacterB, collection.GetParameter("CharacterB")),
                character3 = ToSharedParam(CharacterId.CharacterC, collection.GetParameter("CharacterC")),
                character4 = ToSharedParam(CharacterId.CharacterD, collection.GetParameter("CharacterD")),
            };
        }

        private SharedInternalParameter ToSharedParam(int characterId, InternalParameter parameter)
        {
            if (parameter == null) return default;
            return new SharedInternalParameter
            {
                CharacterId = characterId,
                Morale = parameter.Morale,
            };
        }

        private void EnsureDefaultCharacters()
        {
            EnsureCharacter("CharacterA");
            EnsureCharacter("CharacterB");
            EnsureCharacter("CharacterC");
            EnsureCharacter("CharacterD");
        }

        private void EnsureCharacter(string characterId)
        {
            if (_cache.GetParameter(characterId) != null)
                return;

            _cache.SetParameter(characterId, new InternalParameter());
            Debug.Log($"[MoraleUseCase] Created default morale entry: {characterId}");
        }
    }
}
