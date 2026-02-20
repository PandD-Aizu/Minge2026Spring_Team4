using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.Infrastructure.ExternalServices;
using Minge2026Spring.Scripts.Presenter;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.DI
{
    public class NovelLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<LLMProvider>(Lifetime.Scoped)
                .AsImplementedInterfaces();
            builder.Register<GenerateTextUseCase>(Lifetime.Scoped);
            builder.RegisterEntryPoint<NovelLLMPresenter>();
            
            Debug.Log("NovelLifetimeScope configured.");
        }
    }
}