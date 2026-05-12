namespace parkmeter_management.Domain.Enums
{
    [Flags]
    public enum MachineEventType
    {
        NONE = 0,
        MACHINE_ONLINE = 1,
        MACHINE_OFFLINE = 2,
        MACHINE_RECONNECTED = 4,
        CASH_COLLECTED = 8,
        PAPER_REFILLED = 16,
        DOOR_CLOSED = 32,
    }
}