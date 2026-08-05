using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.Infrastructure.Repositories;
using Minge2026Spring.Scripts.Infrastructure.tmp;
using Minge2026Spring.Scripts.Presenter;
using Minge2026Spring.Scripts.View;
using VContainer;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.DI
{
    public class AchievementLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<AchievementDefaultUIPresenter>();
            builder.RegisterComponentInHierarchy<AchievementDefaultUIView>();

            builder.Register<SceneTransitionUseCase>(Lifetime.Scoped);
            builder.Register<AchievementUseCase>(Lifetime.Scoped);
            builder.Register<ProcessUseCase>(Lifetime.Scoped);
            builder.Register<EndingUseCase>(Lifetime.Scoped);
            builder.Register<GameSaveRepository>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.Register<EndingValueRepository>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.Register<TmpMoraleValueJsonService>(Lifetime.Scoped).AsImplementedInterfaces();
        }
    }
}
