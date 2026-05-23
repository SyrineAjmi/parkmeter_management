namespace parkmeter_management.Domain.Enums
{
    [Flags]
    public enum AlertType
    {
        NONE = 0,
        CASH_BOX_WARN = 1,
        CASH_BOX_FULL = 2,
        PAPER_LOW = 8,
        PAPER_EMPTY = 16,
        HARDWARE_FAILURE = 64,
        DOOR_OPEN = 128,
        CONNECTION_LOST = 256
    }
}
