namespace parkmeter_management.SimulatorCore
{
    public class ParkMeterState
    {
        // identity
        public Guid Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;

        // status
        public string Status { get; set; } = "PENDING_ACTIVATION";
        public bool HasConfig { get; set; } = false;

        // configuration — base
        public int MaxDuration { get; set; } = 0;

        // pricing strategy
        public string StrategyType { get; set; } = "FLAT_RATE";
        public decimal PricePerHour { get; set; } = 0;   // FLAT_RATE
        public decimal PeakRate { get; set; } = 0;        // TIME_OF_DAY
        public decimal OffPeakRate { get; set; } = 0;     // TIME_OF_DAY
        public decimal WeekdayRate { get; set; } = 0;     // WEEKDAY_WEEKEND
        public decimal WeekendRate { get; set; } = 0;     // WEEKDAY_WEEKEND

        // simulation speed
        public int MinTickDelayMs { get; set; } = 15000;
        public int MaxTickDelayMs { get; set; } = 30000;

        // changes per successful transaction
        public int CashFillPerTick { get; set; } = 8;
        public int PaperUsePerTick { get; set; } = 3;

        // physical levels
        public int TransactionCount { get; set; } = 0;
        public int PaperLevel { get; set; } = 100;
        public decimal CashAmount { get; set; } = 0;

        // cash thresholds in DT (demo values — fast for testing)
        public decimal CashWarnThreshold { get; set; } = 20.000m;
        public decimal CashFullThreshold { get; set; } = 40.000m;

        // alert flags (prevent duplicate alerts)
        public bool CashBoxWarnSent { get; set; } = false;
        public bool CashBoxAlertSent { get; set; } = false;
        public bool PaperAlertSent { get; set; } = false;
        public bool PaperEmptyAlertSent { get; set; } = false;

        // hardware/connect flags
        public bool HardwareFailure { get; set; } = false;
        public bool DoorOpen { get; set; } = false;
        public bool IsOffline { get; set; } = false;
        public bool IsConnectionLost { get; set; } = false;
        public bool WasConnectionLost { get; set; } = false;
        public bool IsIntentionalDisconnect { get; set; } = false;

    }


}