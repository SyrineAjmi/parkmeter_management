namespace parkmeter_management.Api.Requests.Parkmeter
{
    public class AddParkmeterRequest
    {
        public string SerialNumber { get; set; } = string.Empty;
        public Guid SiteId { get; set; }
    }
}
