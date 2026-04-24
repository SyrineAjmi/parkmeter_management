namespace parkmeter_management.Domain.Enums
{
    [Flags]
    public enum AlertType
    {
        NONE = 0,
        CASH_BOX_WARN = 1,
        CASH_BOX_FULL = 2,
        PAPER_LOW = 4,
        PAPER_EMPTY = 8,
        HARDWARE_FAILURE = 16,
        DOOR_OPEN = 32,
        POWER_FAILURE = 64,
        CONNECTION_LOST = 128
    }
}