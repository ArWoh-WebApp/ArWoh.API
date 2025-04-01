namespace ArWoh.API.DTOs.AdminDtos
{
    public class RevenueSummaryDTO
    {
        public decimal TotalRevenue { get; set; }
        public Dictionary<string, decimal> MonthlyRevenue { get; set; }
    }
}
