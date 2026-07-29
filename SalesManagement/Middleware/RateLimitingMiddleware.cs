using System.Collections.Concurrent;

namespace SalesManagement.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, (int Count, DateTime LastAttempt)> _attempts = new();
    private const int MaxAttempts = 5;
    private const int LockoutMinutes = 15;

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Só aplica em POST para Account/Login
        if (context.Request.Method == "POST" &&
            context.Request.Path.HasValue &&
            context.Request.Path.Value.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"login:{ip}";

            _attempts.TryGetValue(key, out var entry);

            // Limpa entrada expirada
            if (entry.LastAttempt != default && DateTime.UtcNow > entry.LastAttempt.AddMinutes(LockoutMinutes))
            {
                _attempts.TryRemove(key, out _);
                entry = default;
            }

            if (entry.Count >= MaxAttempts)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Muitas tentativas de login. Tente novamente em 15 minutos.");
                return;
            }

            // Processa a requisição
            await _next(context);

            // Se o login falhou (redirecionou de volta para Login com erro), incrementa
            // Como não temos acesso ao resultado aqui de forma simples, incrementamos no controller
            // Este middleware faz apenas a verificação do bloqueio
        }
        else
        {
            await _next(context);
        }
    }

    public static void RecordFailedAttempt(string ip)
    {
        var key = $"login:{ip}";
        _attempts.AddOrUpdate(key, (1, DateTime.UtcNow), (_, old) => (old.Count + 1, DateTime.UtcNow));
    }
}