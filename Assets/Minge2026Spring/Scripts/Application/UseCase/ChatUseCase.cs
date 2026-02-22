using System.Collections.Generic;
using System.Linq;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.Interface;
using R3;

namespace Minge2026Spring.Scripts.Application.UseCase
{
    public class ChatUseCase
    {
        private readonly IJsonUtilityProvider _jsonUtilityProvider;
        
        private Chapter _chapter;
        private List<ChapterBlock> _chapterBlocks;

        public ReadOnlyReactiveProperty<ChapterBlock> CurrentChapterBlock => _currentChapterBlock.ToReadOnlyReactiveProperty();
        private readonly ReactiveProperty<ChapterBlock> _currentChapterBlock = new ();
        
        public ChatUseCase(IJsonUtilityProvider jsonUtilityProvider)
        {
            _jsonUtilityProvider = jsonUtilityProvider;
        }

        /// <summary>
        /// 章の会話データをロードする
        /// </summary>
        /// <param name="chapterId">章の会話jsonデータへのパス</param>
        public void LoadChapter(string chapterId)
        {
            // 章の会話データをjsonからロードする
            _chapter = _jsonUtilityProvider.ConvertJsonToAnyObject<Chapter>(chapterId);
            if (_chapter?.blocks is null || _chapter.blocks.Length == 0)
                return;
            
            // ブロックに分割
            _chapterBlocks = _chapter.blocks.ToList();
            
            // 最初のブロックをセット
            _currentChapterBlock.Value = _chapterBlocks[0];
        }

        /// <summary>
        /// 次の会話ブロックに進む
        /// </summary>
        /// <param name="userChooseIndex">ユーザーが選んだ選択肢</param>
        public void MoveToNextBlock(int userChooseIndex = -1)
        {
            var currentBlock = _currentChapterBlock.Value;
            
            // 選択肢がある: 遷移先へ移動、選択肢がない: 通常の遷移先へ移動
            string nextBlockId = (userChooseIndex >= 0 && currentBlock.choices?.Length > userChooseIndex) 
                ? currentBlock.choices[userChooseIndex].nextBlockId 
                : currentBlock.nextBlockId;
            
            // 取得したIDに該当するブロックに更新
            _currentChapterBlock.Value = _chapterBlocks.FirstOrDefault(block => block.blockId == nextBlockId);
        }
    }
}