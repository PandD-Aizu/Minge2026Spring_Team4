using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.Presenter;
using Minge2026Spring.Scripts.View;
using VContainer;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.DI
{
    public class TitleLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SceneTransitionUseCase>(Lifetime.Scoped);
            builder.Register<ApplicationStopUseCase>(Lifetime.Scoped);
            
            builder.RegisterEntryPoint<TitleDefaultUIPresenter>();

            builder.RegisterComponentInHierarchy<TitleDefaultUIView>();
        }
    }
}