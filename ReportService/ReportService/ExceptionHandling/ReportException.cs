using System.Runtime.Serialization;
using ReportService.Models.Enums;

namespace ReportService.ExceptionHandling;

public class ReportException : Exception, ISerializable
{
    public ErrorCodeEnum ErrorCode { get; set; }

    public ReportException()
    {
    }

    public ReportException(string message)
        : base(message)
    {
    }

    public ReportException(ErrorCodeEnum errorCode, string message, Exception inner)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }

    public ReportException(string message, Exception inner)
        : this(ErrorCodeEnum.SystemError, message, inner)
    {
    }
}