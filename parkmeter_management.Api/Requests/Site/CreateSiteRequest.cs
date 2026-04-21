namespace parkmeter_management.Api.Requests.Site
{
    public class CreateSiteRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
