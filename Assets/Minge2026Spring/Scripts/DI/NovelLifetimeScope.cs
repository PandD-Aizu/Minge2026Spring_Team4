using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.Infrastructure.ExternalServices;
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
            builder.Register<ChatUseCase>(Lifetime.Scoped);
            builder.RegisterEntryPoint<NovelChatPresenter>();
            builder.RegisterComponentInHierarchy<ChatWindowView>();
        }
    }
}