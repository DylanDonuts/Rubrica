using Microsoft.AspNetCore.Diagnostics;
using Npgsql;

namespace Rubrica.Api;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message) = exception switch
        {
            PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } =>
                (StatusCodes.Status409Conflict, "Numero di telefono già presente"),
            BadHttpRequestException badRequest =>
                (badRequest.StatusCode, "Richiesta non valida"),
            NpgsqlException when context.Request.Path == "/health" =>
                (StatusCodes.Status503ServiceUnavailable, "Database non disponibile"),
            _ => (StatusCodes.Status500InternalServerError, "Impossibile completare l'operazione")
        };
        if (status >= 500)
            logger.LogError(exception, "Errore durante {Method} {Path}", context.Request.Method, context.Request.Path);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ApiError(message), cancellationToken);
        return true;
    }
}
