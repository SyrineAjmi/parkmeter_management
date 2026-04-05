using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Helpers
{
    public static class AlertPriorityHelper
    {
        public static Priority GetPriority(AlertType type) => type switch
        {
            AlertType.CASH_BOX_WARN => Priority.NORMAL,
            AlertType.CASH_BOX_FULL => Priority.HIGH,
            AlertType.CASH_BOX_REMOVED => Priority.NORMAL,
            AlertType.PAPER_LOW => Priority.NORMAL,
            AlertType.PAPER_EMPTY => Priority.HIGH,
            AlertType.PAPER_REFILLED => Priority.LOW,
            AlertType.HARDWARE_FAILURE => Priority.HIGH,
            AlertType.DOOR_OPEN => Priority.NORMAL,
            AlertType.POWER_FAILURE => Priority.HIGH,
            AlertType.CONNECTION_LOST => Priority.NORMAL,
            _ => Priority.LOW
        };
    }
}