namespace parkmeter_management.Domain.Entities

{
    public class Site
    {
        private Site() { }

        public Site(string name, string address, string description, double latitude, double longitude)
        {
            Id = Guid.NewGuid();
            Name = name;
            Address = address;
            Description = description;
            Latitude = latitude;
            Longitude = longitude;
            CreatedAt = DateTime.UtcNow;
        }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = string.Empty;
        public string Address { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public void Update(string name, string address, string description, double latitude, double longitude)
        {
            Name = name;
            Address = address;
            Description = description;
            Latitude = latitude;
            Longitude = longitude;
        }


    }
}

