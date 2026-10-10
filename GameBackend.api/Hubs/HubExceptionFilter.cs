using GameBackend.Application.Common;
using GameBackend.Domain;
using Microsoft.AspNetCore.SignalR;

namespace GameBackend.Api.Hubs;

public class HubExceptionFilter(ILogger<HubExceptionFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex) when (ex is DomainException or NotFoundException or ValidationFailedException)
        {
            throw new HubException(ex.Message);
        }
        catch (Exception ex) when (ex is not HubException)
        {
            logger.LogError(ex, "Hub method {Method} failed", invocationContext.HubMethodName);
            throw new HubException("Внутренняя ошибка сервера");
        }
    }
}