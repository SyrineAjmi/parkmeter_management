namespace parkmeter_management.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid UserId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public Guid? ReferenceId { get; init; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
}