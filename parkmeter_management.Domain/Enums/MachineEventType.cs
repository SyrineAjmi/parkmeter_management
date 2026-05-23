namespace parkmeter_management.Domain.Enums
{
    [Flags]
    public enum MachineEventType
    {
        NONE = 0,
        MACHINE_ONLINE = 1,
        MACHINE_RECONNECTED = 2,
        CASH_COLLECTED = 4,
        PAPER_REFILLED = 8,
        DOOR_CLOSED = 16,
        HARDWARE_FIXED = 32
    }
}