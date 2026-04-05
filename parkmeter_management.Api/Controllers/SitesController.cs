using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.DTOs;
using parkmeter_management.Application.Managers.Interfaces;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SitesController : ControllerBase
    {
        private readonly ISiteManager _siteManager;

        public SitesController(ISiteManager siteManager)
        {
            _siteManager = siteManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sites = await _siteManager.GetAllSites();
            var dtos = sites.Select(s => new SiteDto
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Description = s.Description,
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var site = await _siteManager.GetById(id);
            if (site == null) return NotFound();
            return Ok(new SiteDto
            {
                Id = site.Id,
                Name = site.Name,
                Address = site.Address,
                Description = site.Description,
                CreatedAt = site.CreatedAt,
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateSiteRequest request)
        {
            var id = await _siteManager.CreateSite(
                request.Name,
                request.Address,
                request.Description);
            return Ok(new { Id = id, Message = "Site created successfully" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateSiteRequest request)
        {
            var result = await _siteManager.UpdateSite(
                id, request.Name, request.Address, request.Description);
            if (!result) return NotFound();
            return Ok("Site updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _siteManager.DeleteSite(id);
            if (!result) return NotFound();
            return Ok("Site deleted successfully");
        }
    }

    public class CreateSiteRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateSiteRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}