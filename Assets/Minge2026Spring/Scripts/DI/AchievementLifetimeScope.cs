using Minge2026Spring.Scripts.Application.UseCase;
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
            // シーンのデフォルトUI
            builder.RegisterEntryPoint<AchievementDefaultUIPresenter>();
            builder.RegisterComponentInHierarchy<AchievementDefaultUIView>();
            
            // シーン遷移関係
            builder.Register<SceneTransitionUseCase>(Lifetime.Scoped);
        }
    }
}