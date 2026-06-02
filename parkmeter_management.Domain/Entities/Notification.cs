namespace parkmeter_management.Domain.Entities
{
    public class Notification
    {
        private Notification() { }

        public Notification(Guid userId, string title, string message, string type, Guid? referenceId = null)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Title = title;
            Message = message;
            Type = type;
            ReferenceId = referenceId;
            IsRead = false;
            CreatedAt = DateTime.UtcNow;
        }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid UserId { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;
        public string Type { get; private set; } = string.Empty;
        public bool IsRead { get; private set; } = false;
        public Guid? ReferenceId { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public void MarkAsRead() => IsRead = true;

    }
}