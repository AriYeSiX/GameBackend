using GameBackend.Application.Common;
using GameBackend.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace GameBackend.api.ErrorHandling;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            ConflictException => (StatusCodes.Status409Conflict, exception.Message),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            ValidationFailedException => (StatusCodes.Status400BadRequest, exception.Message),
            DomainException => (StatusCodes.Status409Conflict, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title }
        });
    }
}