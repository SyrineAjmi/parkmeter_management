namespace parkmeter_management.Api.DTOs.Stats
{
    public class OverviewDto
    {
        public int TotalParkmeters { get; set; }
        public int InService { get; set; }
        public int TemporarilyUnavailable { get; set; }
        public int Maintenance { get; set; }
        public int OutOfService { get; set; }
        public int PendingActivation { get; set; }
        public int UnresolvedAlerts { get; set; }
        public int PendingInterventionOrders { get; set; }
    }
}
