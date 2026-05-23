namespace parkmeter_management.Api.DTOs
{
    public class ParkmeterDto

    {
        public Guid Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public Guid SiteId { get; set; }
        public string? SiteName { get; set; }

        public string Status { get; set; } = string.Empty;
        public decimal CashAmount { get; set; }  
        public DateTime CreatedAt { get; set; }
    }
    
}

