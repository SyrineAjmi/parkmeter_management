namespace parkmeter_management.Domain.Enums
{
    public enum ParkMeterEventType
    {
        MACHINE_ONLINE = 1,
        MACHINE_OFFLINE = 2,
        MACHINE_RECONNECTED = 3,
        CASH_COLLECTED = 4,
        PAPER_REFILLED = 5,
        DOOR_CLOSED = 6,
        TRANSACTION_DONE = 7
    }
}