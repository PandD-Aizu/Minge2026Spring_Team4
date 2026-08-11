using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
using R3;
using UnityEngine;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class ChatUseCase
    {
        private readonly IJsonUtilityProvider _jsonUtilityProvider;
        private readonly IGameSaveRepository _gameSaveRepository;

        private Chapter _chapter;
        private List<ChapterBlock> _chapterBlocks;
        private readonly List<ChapterBlock> _reachedChapterBlocks = new();
        private GameSaveData _saveData;
        private string _chapterId;

        public IReadOnlyList<ChapterBlock> ReachedChapterBlocks => _reachedChapterBlocks;

        public ReadOnlyReactiveProperty<ChapterBlock> CurrentChapterBlock =>
            _currentChapterBlock.ToReadOnlyReactiveProperty();

        private readonly ReactiveProperty<ChapterBlock> _currentChapterBlock = new();

        public ReadOnlyReactiveProperty<bool> IsChapterEnded =>
            _isChapterEnded.ToReadOnlyReactiveProperty();

        private readonly ReactiveProperty<bool> _isChapterEnded = new(false);

        public ChatUseCase(
            IJsonUtilityProvider jsonUtilityProvider,
            IFMODSEService seService,
            IGameSaveRepository gameSaveRepository)
        {
            _jsonUtilityProvider = jsonUtilityProvider;
            _gameSaveRepository = gameSaveRepository;
        }

        /// <summary>
        /// 章の会話データと保存済み進捗をロードする
        /// </summary>
        /// <param name="chapterId">章の会話JSONデータへのパス</param>
        public async UniTask LoadChapter(string chapterId)
        {
            // 章の会話データをJSONからロードする
            _isChapterEnded.Value = false;
            _chapterId = chapterId;
            _chapter = await _jsonUtilityProvider.ConvertJsonToAnyObjectAsync<Chapter>(chapterId);
            if (_chapter?.blocks is null || _chapter.blocks.Length == 0)
            {
                _currentChapterBlock.Value = null;
                _isChapterEnded.Value = true;
                return;
            }

            // ブロックへ分割して同じ章の保存進捗を復元する
            _chapterBlocks = _chapter.blocks.ToList();
            _saveData = _gameSaveRepository.Load() ?? new GameSaveData();
            RestoreProgress();
        }

        /// <summary>
        /// 次の会話ブロックに進む
        /// </summary>
        /// <param name="userChooseIndex">ユーザーが選んだ選択肢</param>
        public void MoveToNextBlock(int userChooseIndex = -1)
        {
            var currentBlock = _currentChapterBlock.Value;
            if (currentBlock is null)
            {
                _isChapterEnded.Value = true;
                return;
            }

            // 選択結果または通常遷移から次のブロックIDを決定する
            var nextBlockId = userChooseIndex >= 0 && currentBlock.choices?.Length > userChooseIndex
                ? currentBlock.choices[userChooseIndex].nextBlockId
                : currentBlock.nextBlockId;

            // 遷移先と到達履歴を更新して自動保存する
            _currentChapterBlock.Value = _chapterBlocks.FirstOrDefault(block => block.blockId == nextBlockId);
            if (_currentChapterBlock.Value is null)
            {
                _isChapterEnded.Value = true;
                SaveProgress();
                return;
            }

            AddReachedBlock(_currentChapterBlock.Value);
            SaveProgress();
        }

        /// <summary>
        /// 指定した会話ブロックへ移動する
        /// </summary>
        /// <param name="blockId">移動先のブロックID</param>
        /// <returns>移動先が存在した場合はtrue</returns>
        public bool MoveToBlock(string blockId)
        {
            // ロード済みの章から移動先を検索する
            var destination = _chapterBlocks?.FirstOrDefault(block => block.blockId == blockId);
            if (destination is null)
            {
                Debug.LogError($"[ChatUseCase] Chapter block was not found: {blockId}");
                return false;
            }

            // 終了状態を解除して対象ブロックの表示を開始する
            _isChapterEnded.Value = false;
            _currentChapterBlock.Value = destination;
            AddReachedBlock(destination);
            SaveProgress();
            return true;
        }

        /// <summary>
        /// 到達したエンディングを実績用データへ記録する
        /// </summary>
        /// <param name="endingBlockId">到達したエンディングのブロックID</param>
        public void RecordReachedEnding(string endingBlockId)
        {
            if (string.IsNullOrWhiteSpace(endingBlockId))
                return;

            // actionプロセスが書き込んだプレイ時間とデス数を取り込んでから保存する。
            // 起動前のキャッシュを保存すると、外部プロセスが更新した記録を上書きしてしまう。
            _saveData = _gameSaveRepository.Load() ?? _saveData ?? new GameSaveData();
            var endingIds = (_saveData.reachedEndingIds ?? Array.Empty<string>()).ToList();
            if (!endingIds.Contains(endingBlockId))
            {
                endingIds.Add(endingBlockId);
                _saveData.endingClearCount++;
            }

            _saveData.reachedEndingIds = endingIds.ToArray();
            SaveCurrentProgress();
        }

        /// <summary>
        /// 1周分の会話進行をクリアする。到達済みエンディングは実績用に保持する。
        /// </summary>
        public void ResetProgressAfterClear()
        {
            _saveData ??= _gameSaveRepository.Load() ?? new GameSaveData();
            _saveData.chapterId = null;
            _saveData.currentBlockId = null;
            _saveData.reachedBlockIds = Array.Empty<string>();
            _reachedChapterBlocks.Clear();
            _currentChapterBlock.Value = null;
            _gameSaveRepository.Save(_saveData);
        }

        /// <summary>
        /// 現在の周回だけを最初からやり直す。実績の記録は保持する。
        /// </summary>
        public void ResetProgressForRestart()
        {
            _saveData = _gameSaveRepository.Load() ?? new GameSaveData();
            _saveData.chapterId = null;
            _saveData.currentBlockId = null;
            _saveData.reachedBlockIds = Array.Empty<string>();
            _saveData.extraProgress = new ExtraProgressData();
            _gameSaveRepository.Save(_saveData);
        }

        private void RestoreProgress()
        {
            _reachedChapterBlocks.Clear();

            // 別の章のセーブは現在の章へ適用しない
            var canRestore = _saveData.chapterId == _chapterId;
            if (canRestore && _saveData.reachedBlockIds is not null)
            {
                foreach (var blockId in _saveData.reachedBlockIds)
                {
                    var reachedBlock = _chapterBlocks.FirstOrDefault(block => block.blockId == blockId);
                    if (reachedBlock is not null)
                        _reachedChapterBlocks.Add(reachedBlock);
                }
            }

            // 保存位置が不正な場合は章の先頭から開始する
            var currentBlock = canRestore
                ? _chapterBlocks.FirstOrDefault(block => block.blockId == _saveData.currentBlockId)
                : null;
            currentBlock ??= _chapterBlocks[0];
            AddReachedBlock(currentBlock);
            _currentChapterBlock.Value = currentBlock;
            SaveProgress();
        }

        private void AddReachedBlock(ChapterBlock block)
        {
            if (block is null)
                return;

            // 同じ現在位置の重複通知だけを除外して表示順を保持する
            if (_reachedChapterBlocks.Count == 0 || _reachedChapterBlocks[^1].blockId != block.blockId)
                _reachedChapterBlocks.Add(block);
        }

        private void SaveProgress()
        {
            // 外部ゲームが終了時にプレイ時間・デス数を書き込むため、
            // 起動時に保持したキャッシュでその記録を上書きしないようにする。
            var latestSaveData = _gameSaveRepository.Load();
            if (latestSaveData is not null)
                _saveData = latestSaveData;
            else
                _saveData ??= new GameSaveData();

            SaveCurrentProgress();
        }

        private void SaveCurrentProgress()
        {
            _saveData.chapterId = _chapterId;
            _saveData.currentBlockId = _currentChapterBlock.Value?.blockId;
            _saveData.reachedBlockIds = _reachedChapterBlocks.Select(block => block.blockId).ToArray();
            _saveData.reachedEndingIds ??= Array.Empty<string>();
            _gameSaveRepository.Save(_saveData);
        }
    }
}
