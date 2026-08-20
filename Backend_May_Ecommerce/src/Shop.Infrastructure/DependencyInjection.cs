using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shop.Domain.Repositories;
using Shop.Infrastructure.Persistence;
using Shop.Infrastructure.Repositories;
using Shop.Application.Auth;
using Shop.Application.Common.Caching;
using Shop.Application.Common.Diagnostics;
using Shop.Infrastructure.Auth;
using Shop.Infrastructure.Caching;
using Shop.Infrastructure.Diagnostics;

namespace Shop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                options.InstanceName = "Shop_Session";
            });

        // Singleton: IDistributedCache is registered as a singleton too, and this
        // wrapper holds no per-request state.
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Singleton: one Meter per process. Scoped would create a new set of
        // instruments per request and fragment the exported time series.
        services.AddSingleton<IOrderMetrics, OrderMetrics>();

        // Scoped: it reaches into the scoped DbContext.
        services.AddScoped<IHealthProbe, HealthProbe>();

        return services;
    }
}
