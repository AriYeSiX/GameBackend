using GameBackend.Application.Abstractions;
using GameBackend.Infrastructure.Auth;
using GameBackend.Infrastructure.Leaderboards;
using GameBackend.Infrastructure.Lobbies;
using GameBackend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace GameBackend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));
        
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        
        var ranking = configuration["Leaderboards:Ranking"];

        if (string.Equals(ranking, "Redis", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<ILeaderboardRanking, RedisLeaderboardRanking>();
        else
            services.AddScoped<ILeaderboardRanking, PostgresLeaderboardRanking>();
        
        services.AddSingleton<ILobbyStore, RedisLobbyStore>();
        
        return services;
    }
}