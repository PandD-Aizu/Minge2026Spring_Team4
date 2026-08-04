using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.Infrastructure.Repositories;
using Minge2026Spring.Scripts.Presenter;
using Minge2026Spring.Scripts.View;
using VContainer;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.DI
{
    public class EndingLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // シーンのデフォルト関係
            builder.RegisterEntryPoint<EndingDefaultUIPresenter>();
            builder.RegisterComponentInHierarchy<EndingDefaultUIView>();
            
            // シーン遷移関係
            builder.Register<SceneTransitionUseCase>(Lifetime.Scoped);
            builder.Register<EndingUseCase>(Lifetime.Scoped);
            builder.Register<EndingValueRepository>(Lifetime.Scoped).AsImplementedInterfaces();
        }
    }
}
