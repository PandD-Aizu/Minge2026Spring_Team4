using Minge2026Spring.Scripts.Application.UseCase;
using Minge2026Spring.Scripts.Presenter;
using Minge2026Spring.Scripts.View;
using VContainer;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.DI
{
    public class OptionLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // オプション(音量関係)
            builder.RegisterEntryPoint<OptionSliderPresenter>();
            builder.RegisterComponentInHierarchy<OptionDefaultUIView>();
            
            // シーン遷移関係
            builder.Register<SceneTransitionUseCase>(Lifetime.Scoped);
        }
    }
}