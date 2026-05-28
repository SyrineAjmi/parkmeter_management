namespace parkmeter_management.Domain.Entities

{
    public class Site
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
