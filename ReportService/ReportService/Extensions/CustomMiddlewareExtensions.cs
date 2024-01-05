using ReportService.Extensions.Middlewares;

namespace ReportService.Extensions;

public static class CustomMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogMiddleware(this IApplicationBuilder builder)
        => builder.UseMiddleware<RequestLogMiddleware>();

    public static IApplicationBuilder UseResponseLogMiddleware(this IApplicationBuilder builder)
        => builder.UseMiddleware<ResponseLogMiddleware>();
}