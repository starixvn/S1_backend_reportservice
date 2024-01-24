namespace ReportService.Models.Enums;

public enum ErrorCodeEnum
{
    Success = 0,
    Failed = -1,
    SystemError = 9000,
    InsufficientPoint = -1001,
    RedemptionLimitOverLimit = -1002
}