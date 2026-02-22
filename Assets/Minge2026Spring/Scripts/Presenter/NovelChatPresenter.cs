using System;
using Cysharp.Threading.Tasks;
using Minge2026Spring.Scripts.Application.DTOs;
using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.View;
using R3;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.Presenter
{
    public class NovelChatPresenter : IInitializable, IDisposable
    {
        private readonly ChatUseCase _chatUseCase;
        private readonly ChatWindowView _chatWindowView;
        
        private CompositeDisposable _disposables = new();

        public NovelChatPresenter(ChatUseCase chatUseCase, ChatWindowView chatWindowView)
        {
            _chatUseCase = chatUseCase;
            _chatWindowView = chatWindowView;
        }
        
        public void Initialize()
        {
            _chatUseCase.CurrentChapterBlock
                .Where(block => block is not null)
                .SubscribeAwait(async (block, token) =>
                {
                    await _chatWindowView.AddNewChatObject(block, token);

                    if (block.choices is null || block.choices.Length == 0)
                    {
                        if (block.waitingTime > 0)
                            await UniTask.WaitForSeconds(block.waitingTime, cancellationToken: token);
                    }
                    
                    _chatUseCase.MoveToNextBlock();
                })
                .AddTo(_disposables);
            
            _chatUseCase.LoadChapter("Chapter1");
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}