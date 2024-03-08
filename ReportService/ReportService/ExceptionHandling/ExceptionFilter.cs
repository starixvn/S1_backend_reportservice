using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using ReportService.Models.Enums;

namespace ReportService.ExceptionHandling;

public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<ExceptionFilter>>();
    }

    public void OnException(ExceptionContext context)
    {
        var actionPath = $"{context.RouteData.Values["controller"]}/{context.RouteData.Values["action"]}";

        var code = HttpStatusCode.InternalServerError;

        var exception = context.Exception;
        var result = JsonConvert.SerializeObject(new
        {
            ErrorCode = ErrorCodeEnum.SystemError,
            Message = ErrorCodeEnum.SystemError.ToString(),
            RawErrorCode = exception.HResult.ToString(),
            RawMessage = exception.Message,
            exception.StackTrace
        });

        if (context.Exception.GetType() == typeof(ReportException))
        {
            code = HttpStatusCode.OK;

            var reportException = context.Exception as ReportException;
            var message = reportException?.Message;

            result = JsonConvert.SerializeObject(new
            {
                reportException?.ErrorCode,
                Message = message
            });
            _logger.LogError(exception, message);
        }
        else
        {
            _logger.LogError(context.Exception, actionPath);
        }

        var httpContext = context.HttpContext;

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)code;
        httpContext.Response.WriteAsync(result);

        context.ExceptionHandled = true;

    }
}