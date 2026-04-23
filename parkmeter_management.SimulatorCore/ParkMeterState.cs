namespace parkmeter_management.SimulatorCore
{
    public class ParkMeterState //the memory of one parkmeter
    {
        // identity
        public Guid Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;

        // status 
        public string Status { get; set; } = "PENDING_ACTIVATION"; 
        public bool HasConfig { get; set; } = false; 

        // configuration 
        public decimal PricePerHour { get; set; } = 0;
        public int MaxDuration { get; set; } = 0;

        // simulation speed
        public int MinTickDelayMs { get; set; } = 15000;
        public int MaxTickDelayMs { get; set; } = 30000;

        // changes per succesful transaction
        public int CashFillPerTick { get; set; } = 8;   
        public int PaperUsePerTick { get; set; } = 3;   

        // physical levels
        public int TransactionCount { get; set; } = 0;
        public int PaperLevel { get; set; } = 100; 
        public int CashLevel { get; set; } = 0; 

        // alerts flags (prevent duplicate alerts)
        public bool CashBoxWarnSent { get; set; } = false;
        public bool CashBoxAlertSent { get; set; } = false;
        public bool PaperAlertSent { get; set; } = false;
        public bool PaperEmptyAlertSent { get; set; } = false;
        public bool PowerFailureAlertSent { get; set; } = false;

        // hardware/connect flags
        public bool HardwareFailure { get; set; } = false;
        public bool DoorOpen { get; set; } = false;
        public bool IsOffline { get; set; } = false;
        public bool IsConnectionLost { get; set; } = false; // should i reconnect or stop?
        public bool WasConnectionLost { get; set; } = false; // did i just recover from connection loss?


    }
}