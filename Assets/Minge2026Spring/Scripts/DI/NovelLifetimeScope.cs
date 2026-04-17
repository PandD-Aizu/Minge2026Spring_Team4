using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.Application.Interface;
using Minge2026Spring.Scripts.Domain.DomainService;
using Minge2026Spring.Scripts.Infrastructure.ExternalServices;
using Minge2026Spring.Scripts.Infrastructure.Repositories;
using Minge2026Spring.Scripts.Infrastructure.Tmp;
using Minge2026Spring.Scripts.Presenter;
using Minge2026Spring.Scripts.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.DI
{
    public class NovelLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // ゲーム進行関係
            builder.Register<ChatUseCase>(Lifetime.Scoped);
            builder.Register<FreeChatUseCase>(Lifetime.Scoped);
            builder.Register<MoraleUseCase>(Lifetime.Scoped);
            builder.Register<GameStarterUseCase>(Lifetime.Scoped);
            builder.Register<MoraleCheckService>(Lifetime.Scoped);
            builder.RegisterEntryPoint<NovelChatPresenter>();
            builder.RegisterComponentInHierarchy<ChatWindowView>();

            // Morale関係
            builder.Register<MoraleRepository>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            builder.Register<SharedMemoryService>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            
            // Json関係
            builder.Register<JsonUtilityProvider>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            builder.Register<JsonCreator>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            builder.Register<TmpMoraleValueJsonService>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            
            // LLM関係
            builder.Register<LLMProvider>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            
            // 音声認識関係
            builder.Register<SpeechRecognitionProvider>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            
            // 外部プロセス関係
            builder.Register<ExternalProcessProvider>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            
            // FMOD関係
            builder.Register<FMODAudioInputProvider>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            builder.Register<FMODBGMService>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            builder.Register<FMODSEService>(Lifetime.Scoped)
                .AsImplementedInterfaces();
        }
    }
}