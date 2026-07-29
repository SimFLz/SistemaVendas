namespace SalesManagement.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Previne MIME-sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Impede que o site seja embutido em iframe (clickjacking)
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Ativa filtro XSS do navegador (legado, mas útil)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Controla quanto de referrer é enviado
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // CSP básico — permite scripts inline (Razor) e CDN do jQuery/validate
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self';";

        // Remove permissões desnecessárias do navegador
        context.Response.Headers["Permissions-Policy"] =
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

        await _next(context);
    }
}