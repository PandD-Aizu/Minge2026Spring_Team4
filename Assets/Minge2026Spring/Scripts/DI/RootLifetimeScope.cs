using Minge2026Spring.Scripts.Infrastructure.ExternalServices;
using Minge2026Spring.Scripts.Infrastructure.Repositories;
using VContainer;
using VContainer.Unity;

namespace Minge2026Spring.Scripts.DI
{
    public class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // External services
            builder.Register<JsonUtilityProvider>(Lifetime.Singleton)
                .AsSelf()
                .AsImplementedInterfaces();
            builder.Register<JsonCreator>(Lifetime.Singleton)
                .AsSelf()
                .AsImplementedInterfaces();
            builder.Register<FMODVCAService>(Lifetime.Singleton)
                .AsImplementedInterfaces();

            // Repository
            builder.Register<MoraleRepository>(Lifetime.Singleton);
            builder.Register<FMODSettingsRepository>(Lifetime.Singleton)
                .AsImplementedInterfaces();
        
            // SceneTransition
            builder.Register<SceneTransitionProvider>(Lifetime.Singleton)
                .AsImplementedInterfaces();
        
            // Application Management
            builder.Register<ApplicationStopProvider>(Lifetime.Singleton)
                .AsImplementedInterfaces();

            // 外部プロセスはシーンをまたいで管理し、親ゲーム終了時に確実に回収する。
            builder.Register<ExternalProcessProvider>(Lifetime.Singleton)
                .AsImplementedInterfaces();
        }
    }
}
