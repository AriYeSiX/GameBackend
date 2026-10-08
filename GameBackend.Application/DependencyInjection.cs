using GameBackend.Application.Auth;
using GameBackend.Application.Players;
using GameBackend.Application.Saves;
using Microsoft.Extensions.DependencyInjection;

namespace GameBackend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<PlayerService>();
        services.AddScoped<SaveService>();
        return services;
    }
}