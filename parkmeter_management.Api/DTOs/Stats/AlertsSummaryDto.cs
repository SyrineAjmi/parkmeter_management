namespace parkmeter_management.Api.DTOs.Stats
{
    public class AlertsSummaryDto
    {
        public int TotalUnresolved { get; set; }
        public int Low { get; set; }
        public int High { get; set; }
        public int Normal { get; set; }
        public Dictionary<string, int> ByType { get; set; } = new();
    }
}
