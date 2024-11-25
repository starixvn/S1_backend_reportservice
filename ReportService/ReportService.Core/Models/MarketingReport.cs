namespace ReportService.Core.Models.MarketingReport
{
    public class MarketingReportRequest
    {
        public int SiteId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class MarketingReportResponse
    {
        public string SiteName { get; set; } // 網站名稱
        public int NewRegister { get; set; } // 新註冊用戶數
        public int ActiveUsers { get; set; } // 活躍用戶數
        public int ActiveNewRegister { get; set; } // 活躍新註冊用戶數
        public decimal ActiveNRCR { get; set; } // 活躍新註冊轉化率
        public int ActiveExistingUsers { get; set; } // 活躍舊用戶數
        public int FTDUsers { get; set; } // 首次存款用戶數
        public int TotalDepositUsers { get; set; } // 總存款用戶數
        public decimal TotalFTDAmount { get; set; } // 首次存款總金額
        public decimal TotalDepositAmount { get; set; } // 總存款金額
        public decimal TotalBonusClaimants { get; set; } // 總獎金申領人數
        public decimal TotalBonusAmountClaimed { get; set; } // 總獎金申領金額
        public decimal AvgClaimedBonusPerClaimants { get; set; } // 平均每人申領獎金
    }
}
