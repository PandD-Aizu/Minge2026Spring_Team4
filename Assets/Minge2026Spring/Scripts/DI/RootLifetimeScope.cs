using Minge2026Spring.Scripts.Infrastructure.ExternalServices;
using Minge2026Spring.Scripts.Infrastructure.Repositories;
using VContainer;
using VContainer.Unity;

public class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Repository
        builder.Register<MoraleRepository>(Lifetime.Singleton);
    }
}