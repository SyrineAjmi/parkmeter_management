using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class Session

    {

        private Session() { }

        public Session(Guid parkmeterId)
        {
            Id = Guid.NewGuid();
            ParkmeterId = parkmeterId;
            StartDateTime = DateTime.UtcNow;
            Status = SessionStatus.ACTIVE;
            TotalTransactions = 0;
            TotalAmount = 0;
        }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; private set; }
        public Parkmeter? Parkmeter { get; set; }
        public DateTime StartDateTime { get; private set; } = DateTime.UtcNow;
        public DateTime? EndDateTime { get; private set; }
        public int TotalTransactions { get; private set; } = 0;
        public decimal TotalAmount { get; private  set; } = 0;
        public SessionStatus Status { get; private set; }
            = SessionStatus.ACTIVE;

        public void Close()
        {
            Status = SessionStatus.CLOSED;
            EndDateTime = DateTime.UtcNow;
        }

        public void AddTransaction(decimal amount)
        {
            TotalTransactions++;
            TotalAmount += amount;
        }
    }
}
